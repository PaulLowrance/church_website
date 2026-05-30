using ChurchWebsite.Core.Interfaces;
using FastEndpoints;

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
    IFileStorageService fileStorage) : Endpoint<UpdatePodcastEpisodeRequest, PodcastEpisodeDto>
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
            await fileStorage.DeleteAudioFileAsync(episode.AudioFilePath, ct);
            episode.AudioFilePath = await fileStorage.SaveAudioFileAsync(req.AudioFile.OpenReadStream(), req.AudioFile.FileName, ct);
            episode.AudioFileName = req.AudioFile.FileName;
            episode.AudioFileSize = req.AudioFile.Length;
            episode.AudioContentType = req.AudioFile.ContentType ?? "audio/mpeg";
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
