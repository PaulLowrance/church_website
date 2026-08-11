using ChurchWebsite.Core;
using ChurchWebsite.Core.Entities;
using ChurchWebsite.Core.Interfaces;
using FastEndpoints;
using Microsoft.Extensions.Configuration;

namespace ChurchWebsite.Api.Endpoints.Podcast;

public class CreatePodcastEpisodeRequest
{
    public string Title { get; set; } = string.Empty;
    public string SpeakerName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SeriesName { get; set; }
    public DateTime PublishedAt { get; set; }
    public string? Tags { get; set; }
    public IFormFile AudioFile { get; set; } = null!;
}

public class CreatePodcastEpisodeResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class CreatePodcastEpisodeEndpoint(
    IPodcastEpisodeRepository repo,
    IFileStorageService fileStorage,
    ITranscriptionService transcription,
    IConfiguration configuration,
    ILogger<CreatePodcastEpisodeEndpoint> logger) : Endpoint<CreatePodcastEpisodeRequest, CreatePodcastEpisodeResponse>
{
    public override void Configure()
    {
        Post("/api/podcast/episodes");
        Roles("Admin");
        AllowFileUploads();
    }

    public override async Task HandleAsync(CreatePodcastEpisodeRequest req, CancellationToken ct)
    {
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

        var audioOptions = AudioUploadValidator.BuildOptions(configuration);
        var (audioOk, audioError) = AudioUploadValidator.Validate(
            req.AudioFile?.FileName,
            req.AudioFile?.ContentType,
            req.AudioFile?.Length ?? 0,
            audioOptions);
        if (!audioOk)
        {
            AddError(r => r.AudioFile, audioError!);
            await Send.ErrorsAsync(statusCode: 400, cancellation: ct);
            return;
        }

        var audio = req.AudioFile!;
        var filePath = await fileStorage.SaveAudioFileAsync(audio.OpenReadStream(), audio.FileName, ct);

        var episode = new PodcastEpisode
        {
            Id = Guid.NewGuid(),
            Title = req.Title.Trim(),
            SpeakerName = req.SpeakerName.Trim(),
            Description = req.Description?.Trim(),
            SeriesName = req.SeriesName?.Trim(),
            AudioFilePath = filePath,
            AudioFileName = audio.FileName,
            AudioFileSize = audio.Length,
            AudioContentType = audio.ContentType ?? "audio/mpeg",
            PublishedAt = req.PublishedAt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(req.PublishedAt, DateTimeKind.Utc)
                : req.PublishedAt.ToUniversalTime(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Tags = ParseTags(req.Tags)
        };

        await repo.CreateAsync(episode);

        await SubmitTranscriptionAsync(episode, transcription, repo, logger, ct);

        await Send.OkAsync(new CreatePodcastEpisodeResponse
        {
            Id = episode.Id,
            Title = episode.Title
        }, cancellation: ct);
    }

    private static async Task SubmitTranscriptionAsync(
        PodcastEpisode episode,
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

        episode.UpdatedAt = DateTime.UtcNow;
        await repo.UpdateAsync(episode);
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
