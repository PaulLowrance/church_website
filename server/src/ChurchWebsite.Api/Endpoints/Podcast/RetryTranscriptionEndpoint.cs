using ChurchWebsite.Core.Interfaces;
using FastEndpoints;

namespace ChurchWebsite.Api.Endpoints.Podcast;

public class RetryTranscriptionRequest
{
    public Guid Id { get; set; }
}

public class RetryTranscriptionEndpoint(
    IPodcastEpisodeRepository repo,
    IFileStorageService fileStorage,
    ITranscriptionService transcription,
    ILogger<RetryTranscriptionEndpoint> logger) : Endpoint<RetryTranscriptionRequest, PodcastEpisodeDto>
{
    public override void Configure()
    {
        Post("/api/podcast/episodes/{id}/retry-transcription");
        Roles("Admin");
    }

    public override async Task HandleAsync(RetryTranscriptionRequest req, CancellationToken ct)
    {
        var episode = await repo.GetByIdAsync(req.Id);
        if (episode is null)
        {
            await Send.NotFoundAsync(cancellation: ct);
            return;
        }

        if (episode.TranscriptStatus != "error")
        {
            ThrowError("Transcription can only be retried when it has failed.");
            return;
        }

        try
        {
            var transcriptId = await transcription.SubmitAsync(episode.AudioFilePath, ct);
            episode.AssemblyAiTranscriptId = transcriptId;
            episode.TranscriptStatus = "queued";
            episode.TranscriptError = null;
            episode.UpdatedAt = DateTime.UtcNow;
            await repo.UpdateAsync(episode);

            logger.LogInformation("Retried transcription for episode {EpisodeId}, new transcript {TranscriptId}",
                episode.Id, transcriptId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Retry transcription failed for episode {EpisodeId}", episode.Id);
            ThrowError($"Transcription submission failed: {ex.Message}");
            return;
        }

        await Send.OkAsync(PodcastEpisodeMapper.ToDto(episode, fileStorage), cancellation: ct);
    }
}
