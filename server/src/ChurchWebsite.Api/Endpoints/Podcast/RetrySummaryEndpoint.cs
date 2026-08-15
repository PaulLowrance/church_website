using ChurchWebsite.Core.Interfaces;
using FastEndpoints;

namespace ChurchWebsite.Api.Endpoints.Podcast;

public class RetrySummaryRequest
{
    public Guid Id { get; set; }
}

public class RetrySummaryEndpoint(
    IPodcastEpisodeRepository repo,
    IFileStorageService fileStorage,
    ITranscriptionService transcription,
    ILogger<RetrySummaryEndpoint> logger) : Endpoint<RetrySummaryRequest, PodcastEpisodeDto>
{
    public override void Configure()
    {
        Post("/api/podcast/episodes/{id}/retry-summary");
        Roles("Admin");
    }

    public override async Task HandleAsync(RetrySummaryRequest req, CancellationToken ct)
    {
        var episode = await repo.GetByIdAsync(req.Id);
        if (episode is null)
        {
            await Send.NotFoundAsync(cancellation: ct);
            return;
        }

        if (episode.TranscriptStatus != "completed" || string.IsNullOrWhiteSpace(episode.TranscriptFilePath))
        {
            ThrowError("A completed transcript is required before summarization can be retried.");
            return;
        }

        var transcriptText = await fileStorage.ReadTranscriptFileAsync(episode.TranscriptFilePath, ct);
        if (string.IsNullOrWhiteSpace(transcriptText))
        {
            ThrowError("Transcript file is missing or empty; cannot retry summarization.");
            return;
        }

        episode.SummaryStatus = "processing";
        episode.SummaryError = null;
        episode.UpdatedAt = DateTime.UtcNow;
        await repo.UpdateAsync(episode);

        try
        {
            var summary = await transcription.SummarizeAsync(transcriptText, ct);
            if (!string.IsNullOrWhiteSpace(summary))
            {
                episode.Description = summary;
                episode.SummaryStatus = "completed";
                episode.SummaryError = null;
                episode.UpdatedAt = DateTime.UtcNow;
                await repo.UpdateAsync(episode);

                logger.LogInformation("Retried summary for episode {EpisodeId}", episode.Id);
            }
            else
            {
                episode.SummaryStatus = "error";
                episode.SummaryError = "LLM Gateway returned an empty summary.";
                episode.UpdatedAt = DateTime.UtcNow;
                await repo.UpdateAsync(episode);

                ThrowError(episode.SummaryError);
                return;
            }
        }
        catch (Exception ex)
        {
            episode.SummaryStatus = "error";
            episode.SummaryError = ex.Message;
            episode.UpdatedAt = DateTime.UtcNow;
            await repo.UpdateAsync(episode);

            logger.LogError(ex, "Retry summary failed for episode {EpisodeId}", episode.Id);
            ThrowError($"Summary generation failed: {ex.Message}");
            return;
        }

        await Send.OkAsync(PodcastEpisodeMapper.ToDto(episode, fileStorage), cancellation: ct);
    }
}
