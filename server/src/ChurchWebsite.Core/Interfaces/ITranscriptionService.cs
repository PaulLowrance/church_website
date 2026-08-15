namespace ChurchWebsite.Core.Interfaces;

public record TranscriptionResult(string Status, string? Text, string? Error);

public interface ITranscriptionService
{
    Task<string> SubmitAsync(string audioFilePath, CancellationToken ct = default);
    Task<TranscriptionResult> GetResultAsync(string transcriptId, CancellationToken ct = default);
    Task<string> SummarizeAsync(string transcriptText, CancellationToken ct = default);
}
