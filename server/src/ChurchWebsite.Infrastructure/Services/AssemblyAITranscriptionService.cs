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

    private int MaxSummaryTokens => int.TryParse(configuration["AssemblyAI:MaxSummaryTokens"], out var tokens) ? tokens : 500;

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

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("LLM Gateway summarization failed with HTTP {StatusCode}: {Body}",
                (int)response.StatusCode, body);
            throw new InvalidOperationException($"LLM Gateway summarization failed with HTTP {(int)response.StatusCode}.");
        }

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var content = result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

        return content?.Trim() ?? string.Empty;
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
