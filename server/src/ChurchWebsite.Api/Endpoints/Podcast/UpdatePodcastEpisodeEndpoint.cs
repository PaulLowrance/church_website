using ChurchWebsite.Core;
using ChurchWebsite.Core.Interfaces;
using FastEndpoints;
using Microsoft.Extensions.Configuration;

namespace ChurchWebsite.Api.Endpoints.Podcast;

public class UpdatePodcastEpisodeRequest
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string SpeakerName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SeriesName { get; set; }
    public DateTime PublishedAt { get; set; }
    public string? Tags { get; set; }
    public IFormFile? AudioFile { get; set; }
}

public class UpdatePodcastEpisodeEndpoint(
    IPodcastEpisodeRepository repo,
    IFileStorageService fileStorage,
    ITranscriptionService transcription,
    IConfiguration configuration,
    ILogger<UpdatePodcastEpisodeEndpoint> logger) : Endpoint<UpdatePodcastEpisodeRequest, PodcastEpisodeDto>
{
    public override void Configure()
    {
        Put("/api/podcast/episodes/{id}");
        Roles("Admin");
        AllowFileUploads();
    }

    public override async Task HandleAsync(UpdatePodcastEpisodeRequest req, CancellationToken ct)
    {
        var episode = await repo.GetByIdAsync(req.Id);
        if (episode is null)
        {
            await Send.NotFoundAsync(cancellation: ct);
            return;
        }

        if (string.IsNullOrWhiteSpace(req.Title))
        {
            ThrowError("Title is required");
            return;
        }

        if (string.IsNullOrWhiteSpace(req.SpeakerName))
        {
            ThrowError("Speaker name is required");
            return;
        }

        if (req.AudioFile is not null && req.AudioFile.Length > 0)
        {
            var audioOptions = AudioUploadValidator.BuildOptions(configuration);
            var (audioOk, audioError) = AudioUploadValidator.Validate(
                req.AudioFile.FileName,
                req.AudioFile.ContentType,
                req.AudioFile.Length,
                audioOptions);
            if (!audioOk)
            {
                AddError(r => r.AudioFile, audioError!);
                await Send.ErrorsAsync(statusCode: 400, cancellation: ct);
                return;
            }

            await fileStorage.DeleteAudioFileAsync(episode.AudioFilePath, ct);
            episode.AudioFilePath = await fileStorage.SaveAudioFileAsync(req.AudioFile.OpenReadStream(), req.AudioFile.FileName, ct);
            episode.AudioFileName = req.AudioFile.FileName;
            episode.AudioFileSize = req.AudioFile.Length;
            episode.AudioContentType = req.AudioFile.ContentType ?? "audio/mpeg";

            if (!string.IsNullOrWhiteSpace(episode.TranscriptFilePath))
            {
                await fileStorage.DeleteTranscriptFileAsync(episode.TranscriptFilePath, ct);
            }
            episode.TranscriptFilePath = null;
            episode.TranscriptError = null;
            episode.SummaryStatus = "none";
            episode.SummaryError = null;
            await SubmitTranscriptionAsync(episode, transcription, repo, logger, ct);
        }

        episode.Title = req.Title.Trim();
        episode.SpeakerName = req.SpeakerName.Trim();
        episode.Description = req.Description?.Trim();
        episode.SeriesName = req.SeriesName?.Trim();
        episode.PublishedAt = req.PublishedAt.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(req.PublishedAt, DateTimeKind.Utc)
            : req.PublishedAt.ToUniversalTime();
        episode.UpdatedAt = DateTime.UtcNow;
        episode.Tags = ParseTags(req.Tags);

        await repo.UpdateAsync(episode);

        await Send.OkAsync(PodcastEpisodeMapper.ToDto(episode, fileStorage), cancellation: ct);
    }

    private static async Task SubmitTranscriptionAsync(
        ChurchWebsite.Core.Entities.PodcastEpisode episode,
        ITranscriptionService transcription,
        IPodcastEpisodeRepository repo,
        ILogger logger,
        CancellationToken ct)
    {
        try
        {
            var transcriptId = await transcription.SubmitAsync(episode.AudioFilePath, ct);
            episode.AssemblyAiTranscriptId = transcriptId;
            episode.TranscriptStatus = "queued";
            episode.TranscriptError = null;
            episode.SummaryStatus = "none";
            episode.SummaryError = null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to submit transcription for episode {EpisodeId}", episode.Id);
            episode.TranscriptStatus = "error";
            episode.TranscriptError = $"Transcription submission failed: {ex.Message}";
        }
    }

    private static List<string> ParseTags(string? tagsInput)
    {
        if (string.IsNullOrWhiteSpace(tagsInput))
            return [];

        return tagsInput
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct()
            .ToList();
    }
}
