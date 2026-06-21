using ChurchWebsite.Core.Entities;
using ChurchWebsite.Core.Interfaces;
using FastEndpoints;

namespace ChurchWebsite.Api.Endpoints.Podcast;

public class PodcastEpisodeDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string SpeakerName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SeriesName { get; set; }
    public string AudioUrl { get; set; } = string.Empty;
    public string AudioFileName { get; set; } = string.Empty;
    public long AudioFileSize { get; set; }
    public string AudioContentType { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Tags { get; set; } = [];
}

public static class PodcastEpisodeMapper
{
    public static PodcastEpisodeDto ToDto(PodcastEpisode episode, IFileStorageService fileStorage)
    {
        return new PodcastEpisodeDto
        {
            Id = episode.Id,
            Title = episode.Title,
            SpeakerName = episode.SpeakerName,
            Description = episode.Description,
            SeriesName = episode.SeriesName,
            AudioUrl = fileStorage.GetPublicUrl(episode.AudioFilePath),
            AudioFileName = episode.AudioFileName,
            AudioFileSize = episode.AudioFileSize,
            AudioContentType = episode.AudioContentType,
            PublishedAt = episode.PublishedAt,
            CreatedAt = episode.CreatedAt,
            Tags = episode.Tags
        };
    }
}
