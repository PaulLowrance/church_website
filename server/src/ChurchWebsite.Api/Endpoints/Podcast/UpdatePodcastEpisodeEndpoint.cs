using ChurchWebsite.Core;
using ChurchWebsite.Core.Interfaces;
using FastEndpoints;
using Microsoft.Extensions.Configuration;

namespace ChurchWebsite.Api.Endpoints.Podcast;

public class UpdatePodcastEpisodeRequest
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? SpeakerTitle { get; set; }
    public string SpeakerName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Scripture { get; set; }
    public string? SeriesName { get; set; }
    public DateTime PublishedAt { get; set; }
    public string? Tags { get; set; }
    public IFormFile? AudioFile { get; set; }
    public IFormFile? CoverImageFile { get; set; }
}

public class UpdatePodcastEpisodeEndpoint(
    IPodcastEpisodeRepository repo,
    IFileStorageService fileStorage,
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

        var audioReplaced = req.AudioFile is not null && req.AudioFile.Length > 0;
        var coverImageReplaced = req.CoverImageFile is not null && req.CoverImageFile.Length > 0;

        if (coverImageReplaced)
        {
            var cover = req.CoverImageFile!;
            var imageOptions = ImageUploadValidator.BuildOptions(configuration);
            var (imageOk, imageError) = ImageUploadValidator.Validate(
                cover.FileName,
                cover.ContentType,
                cover.Length,
                imageOptions);
            if (!imageOk)
            {
                AddError(r => r.CoverImageFile, imageError!);
                await Send.ErrorsAsync(statusCode: 400, cancellation: ct);
                return;
            }

            if (!string.IsNullOrWhiteSpace(episode.CoverImagePath))
            {
                await fileStorage.DeleteImageFileAsync(episode.CoverImagePath, ct);
            }
            episode.CoverImagePath = await fileStorage.SaveImageFileAsync(cover.OpenReadStream(), cover.FileName, ct);
        }

        if (audioReplaced)
        {
            var audio = req.AudioFile!;
            var audioOptions = AudioUploadValidator.BuildOptions(configuration);
            var (audioOk, audioError) = AudioUploadValidator.Validate(
                audio.FileName,
                audio.ContentType,
                audio.Length,
                audioOptions);
            if (!audioOk)
            {
                AddError(r => r.AudioFile, audioError!);
                await Send.ErrorsAsync(statusCode: 400, cancellation: ct);
                return;
            }

            await fileStorage.DeleteAudioFileAsync(episode.AudioFilePath, ct);
            episode.AudioFilePath = await fileStorage.SaveAudioFileAsync(audio.OpenReadStream(), audio.FileName, ct);
            episode.AudioFileName = audio.FileName;
            episode.AudioFileSize = audio.Length;
            episode.AudioContentType = audio.ContentType ?? "audio/mpeg";

            if (!string.IsNullOrWhiteSpace(episode.TranscriptFilePath))
            {
                await fileStorage.DeleteTranscriptFileAsync(episode.TranscriptFilePath, ct);
            }
            episode.TranscriptFilePath = null;
            episode.AssemblyAiTranscriptId = null;
            episode.TranscriptError = null;
            episode.TranscriptStatus = TranscriptionStatuses.PendingSubmit;
            episode.SummaryStatus = TranscriptionStatuses.None;
            episode.SummaryError = null;

            logger.LogInformation("Episode {EpisodeId} audio replaced; queued for transcription submission.", episode.Id);
        }

        episode.Title = req.Title.Trim();
        episode.SpeakerTitle = string.IsNullOrWhiteSpace(req.SpeakerTitle) ? null : req.SpeakerTitle.Trim();
        episode.SpeakerName = req.SpeakerName.Trim();
        episode.Description = req.Description?.Trim();
        episode.Scripture = string.IsNullOrWhiteSpace(req.Scripture) ? null : req.Scripture.Trim();
        episode.SeriesName = req.SeriesName?.Trim();
        episode.PublishedAt = req.PublishedAt.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(req.PublishedAt, DateTimeKind.Utc)
            : req.PublishedAt.ToUniversalTime();
        episode.UpdatedAt = DateTime.UtcNow;
        episode.Tags = ParseTags(req.Tags);

        await repo.UpdateAsync(episode);

        var abbr = PodcastEpisodeMapper.LoadTitleAbbreviations(configuration);
        await Send.ResponseAsync(
            PodcastEpisodeMapper.ToDto(episode, fileStorage, abbr, configuration),
            StatusCodes.Status202Accepted,
            cancellation: ct);
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
