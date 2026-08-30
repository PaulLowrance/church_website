using ChurchWebsite.Core;
using ChurchWebsite.Core.Entities;
using ChurchWebsite.Core.Interfaces;
using FastEndpoints;
using Microsoft.Extensions.Configuration;

namespace ChurchWebsite.Api.Endpoints.Podcast;

public class CreatePodcastEpisodeRequest
{
    public string Title { get; set; } = string.Empty;
    public string? SpeakerTitle { get; set; }
    public string SpeakerName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Scripture { get; set; }
    public string? SeriesName { get; set; }
    public DateTime PublishedAt { get; set; }
    public string? Tags { get; set; }
    public IFormFile AudioFile { get; set; } = null!;
    public IFormFile? CoverImageFile { get; set; }
}

public class CreatePodcastEpisodeResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class CreatePodcastEpisodeEndpoint(
    IPodcastEpisodeRepository repo,
    IFileStorageService fileStorage,
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
        var audioFilePath = await fileStorage.SaveAudioFileAsync(audio.OpenReadStream(), audio.FileName, ct);

        string? coverImagePath = null;
        if (req.CoverImageFile is not null && req.CoverImageFile.Length > 0)
        {
            var imageOptions = ImageUploadValidator.BuildOptions(configuration);
            var (imageOk, imageError) = ImageUploadValidator.Validate(
                req.CoverImageFile.FileName,
                req.CoverImageFile.ContentType,
                req.CoverImageFile.Length,
                imageOptions);
            if (!imageOk)
            {
                AddError(r => r.CoverImageFile, imageError!);
                await Send.ErrorsAsync(statusCode: 400, cancellation: ct);
                return;
            }

            coverImagePath = await fileStorage.SaveImageFileAsync(req.CoverImageFile.OpenReadStream(), req.CoverImageFile.FileName, ct);
        }

        var episode = new PodcastEpisode
        {
            Id = Guid.NewGuid(),
            Title = req.Title.Trim(),
            SpeakerTitle = string.IsNullOrWhiteSpace(req.SpeakerTitle) ? null : req.SpeakerTitle.Trim(),
            SpeakerName = req.SpeakerName.Trim(),
            Description = req.Description?.Trim(),
            Scripture = string.IsNullOrWhiteSpace(req.Scripture) ? null : req.Scripture.Trim(),
            SeriesName = req.SeriesName?.Trim(),
            AudioFilePath = audioFilePath,
            CoverImagePath = coverImagePath,
            AudioFileName = audio.FileName,
            AudioFileSize = audio.Length,
            AudioContentType = audio.ContentType ?? "audio/mpeg",
            PublishedAt = req.PublishedAt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(req.PublishedAt, DateTimeKind.Utc)
                : req.PublishedAt.ToUniversalTime(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Tags = ParseTags(req.Tags),
            TranscriptStatus = TranscriptionStatuses.PendingSubmit,
            SummaryStatus = TranscriptionStatuses.None
        };

        await repo.CreateAsync(episode);

        logger.LogInformation("Saved episode {EpisodeId}, queued for transcription submission.", episode.Id);

        var response = new CreatePodcastEpisodeResponse
        {
            Id = episode.Id,
            Title = episode.Title
        };

        await Send.ResponseAsync(response, StatusCodes.Status202Accepted, cancellation: ct);
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
