using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ChurchWebsite.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChurchWebsite.Infrastructure.Services;

public class AssemblyAITranscriptionService(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<AssemblyAITranscriptionService> logger) : ITranscriptionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private string ApiKey => configuration["AssemblyAI:ApiKey"] ?? string.Empty;

    private string BaseUrl => (configuration["AssemblyAI:BaseUrl"] ?? "https://api.assemblyai.com").TrimEnd('/');

    private string LlmGatewayUrl =>
        (configuration["AssemblyAI:LlmGatewayUrl"] ?? "https://llm-gateway.assemblyai.com").TrimEnd('/');

    private string[] SpeechModels => configuration.GetSection("AssemblyAI:SpeechModels").Get<string[]>()
        ?? ["universal-3-5-pro", "universal-2"];

    private string LlmModel => configuration["AssemblyAI:LlmModel"] ?? "gpt-5-mini";

    private string SummaryPrompt => configuration["AssemblyAI:SummaryPrompt"]
        ?? "Write a concise, reverent 2-4 sentence summary of this sermon transcript for a church podcast episode description. Reflect the main theme and any key scripture references. Plain prose, no markdown.";

    private int MaxSummaryTokens => int.TryParse(configuration["AssemblyAI:MaxSummaryTokens"], out var tokens) ? tokens : 2000;

    public async Task<string> SubmitAsync(string audioFilePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException("AssemblyAI:ApiKey is not configured.");
        }

        var uploadUrl = await UploadFileAsync(audioFilePath, ct);

        using var client = CreateClient();
        var payload = new
        {
            audio_url = uploadUrl,
            speech_models = SpeechModels
        };

        using var response = await client.PostAsJsonAsync($"{BaseUrl}/v2/transcript", payload, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        if (!result.TryGetProperty("id", out var idProperty))
        {
            throw new InvalidOperationException("AssemblyAI did not return a transcript id.");
        }

        return idProperty.GetString() ?? throw new InvalidOperationException("AssemblyAI transcript id was empty.");
    }

    public async Task<TranscriptionResult> GetResultAsync(string transcriptId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            return new TranscriptionResult("error", null, "AssemblyAI:ApiKey is not configured.");
        }

        using var client = CreateClient();
        using var response = await client.GetAsync($"{BaseUrl}/v2/transcript/{transcriptId}", ct);

        if (!response.IsSuccessStatusCode)
        {
            return new TranscriptionResult("error", null, $"AssemblyAI poll returned HTTP {(int)response.StatusCode}.");
        }

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);

        var status = result.TryGetProperty("status", out var s) ? s.GetString() ?? "error" : "error";
        var text = result.TryGetProperty("text", out var t) ? t.GetString() : null;
        var error = result.TryGetProperty("error", out var e) ? e.GetString() : null;

        return new TranscriptionResult(status, text, error);
    }

    public async Task<string> SummarizeAsync(string transcriptText, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException("AssemblyAI:ApiKey is not configured.");
        }

        var payload = new
        {
            model = LlmModel,
            messages = new[]
            {
                new { role = "system", content = SummaryPrompt },
                new { role = "user", content = transcriptText }
            },
            max_tokens = MaxSummaryTokens
        };

        using var client = CreateClient();
        using var response = await client.PostAsJsonAsync($"{LlmGatewayUrl}/v1/chat/completions", payload, ct);

        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("LLM Gateway summarization failed with HTTP {StatusCode}: {Body}",
                (int)response.StatusCode, body);
            throw new InvalidOperationException($"LLM Gateway summarization failed with HTTP {(int)response.StatusCode}.");
        }

        JsonElement result;
        try
        {
            result = JsonDocument.Parse(body).RootElement;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "LLM Gateway returned unparseable JSON: {Body}", Truncate(body));
            throw new InvalidOperationException("LLM Gateway returned an invalid response.", ex);
        }

        var requestId = result.TryGetProperty("request_id", out var rid) ? rid.GetString() : null;

        if (!result.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
        {
            logger.LogError("LLM Gateway returned no choices (request_id {RequestId}): {Body}", requestId, Truncate(body));
            throw new InvalidOperationException("LLM Gateway returned an invalid response.");
        }

        var choice = choices[0];
        var finishReason = choice.TryGetProperty("finish_reason", out var fr) ? fr.GetString() : null;
        var message = choice.TryGetProperty("message", out var m) ? m : default;

        var content = ExtractContent(message);
        if (!string.IsNullOrWhiteSpace(content))
        {
            return content.Trim();
        }

        logger.LogWarning(
            "LLM Gateway returned empty content (request_id {RequestId}, model {Model}, finish_reason {FinishReason}): {Body}",
            requestId, LlmModel, finishReason, Truncate(body));
        return string.Empty;
    }

    private static string? ExtractContent(JsonElement message)
    {
        if (message.ValueKind != JsonValueKind.Object ||
            !message.TryGetProperty("content", out var content) ||
            content.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (content.ValueKind == JsonValueKind.String)
        {
            var text = content.GetString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        // Newer API shapes may return content as a list of content parts.
        if (content.ValueKind == JsonValueKind.Array)
        {
            var sb = new StringBuilder();
            foreach (var part in content.EnumerateArray())
            {
                if (part.ValueKind == JsonValueKind.String)
                {
                    sb.Append(part.GetString());
                }
                else if (part.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                {
                    sb.Append(text.GetString());
                }
            }

            var joined = sb.ToString();
            if (!string.IsNullOrWhiteSpace(joined))
            {
                return joined;
            }
        }

        return null;
    }

    private static string Truncate(string value, int maxLength = 4000)
    {
        return value.Length <= maxLength ? value : value[..maxLength] + "...(truncated)";
    }

    private async Task<string> UploadFileAsync(string audioFilePath, CancellationToken ct)
    {
        await using var fileStream = new FileStream(
            audioFilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        using var content = new StreamContent(fileStream);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        using var client = CreateClient();
        using var response = await client.PostAsync($"{BaseUrl}/v2/upload", content, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        if (!result.TryGetProperty("upload_url", out var urlProperty))
        {
            throw new InvalidOperationException("AssemblyAI upload did not return an upload_url.");
        }

        return urlProperty.GetString() ?? throw new InvalidOperationException("AssemblyAI upload_url was empty.");
    }

    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("AssemblyAI");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ApiKey);
        return client;
    }
}
