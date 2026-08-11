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
        episode.SpeakerName = req.SpeakerName.Trim();
        episode.Description = req.Description?.Trim();
        episode.SeriesName = req.SeriesName?.Trim();
        episode.PublishedAt = req.PublishedAt.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(req.PublishedAt, DateTimeKind.Utc)
            : req.PublishedAt.ToUniversalTime();
        episode.UpdatedAt = DateTime.UtcNow;
        episode.Tags = ParseTags(req.Tags);

        await repo.UpdateAsync(episode);

        await Send.ResponseAsync(
            PodcastEpisodeMapper.ToDto(episode, fileStorage),
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
