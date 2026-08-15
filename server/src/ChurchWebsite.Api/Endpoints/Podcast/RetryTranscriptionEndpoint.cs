using ChurchWebsite.Core;
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

        if (episode.TranscriptStatus != TranscriptionStatuses.Error)
        {
            ThrowError("Transcription can only be retried when it has failed.");
            return;
        }

        episode.AssemblyAiTranscriptId = null;
        episode.TranscriptError = null;
        episode.TranscriptStatus = TranscriptionStatuses.PendingSubmit;
        episode.UpdatedAt = DateTime.UtcNow;
        await repo.UpdateAsync(episode);

        logger.LogInformation("Episode {EpisodeId} queued for transcription retry.", episode.Id);

        await Send.ResponseAsync(
            PodcastEpisodeMapper.ToDto(episode, fileStorage),
            StatusCodes.Status202Accepted,
            cancellation: ct);
    }
}
