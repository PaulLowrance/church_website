using ChurchWebsite.Core.Entities;
using ChurchWebsite.Core.Interfaces;
using FastEndpoints;

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
    IFileStorageService fileStorage) : Endpoint<CreatePodcastEpisodeRequest, CreatePodcastEpisodeResponse>
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

        if (req.AudioFile is null || req.AudioFile.Length == 0)
        {
            ThrowError("Audio file is required");
            return;
        }

        var filePath = await fileStorage.SaveAudioFileAsync(req.AudioFile.OpenReadStream(), req.AudioFile.FileName, ct);

        var episode = new PodcastEpisode
        {
            Id = Guid.NewGuid(),
            Title = req.Title.Trim(),
            SpeakerName = req.SpeakerName.Trim(),
            Description = req.Description?.Trim(),
            SeriesName = req.SeriesName?.Trim(),
            AudioFilePath = filePath,
            AudioFileName = req.AudioFile.FileName,
            AudioFileSize = req.AudioFile.Length,
            AudioContentType = req.AudioFile.ContentType ?? "audio/mpeg",
            PublishedAt = req.PublishedAt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(req.PublishedAt, DateTimeKind.Utc)
                : req.PublishedAt.ToUniversalTime(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Tags = ParseTags(req.Tags)
        };

        await repo.CreateAsync(episode);

        await Send.OkAsync(new CreatePodcastEpisodeResponse
        {
            Id = episode.Id,
            Title = episode.Title
        }, cancellation: ct);
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
