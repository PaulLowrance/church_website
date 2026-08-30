using ChurchWebsite.Core;
using ChurchWebsite.Core.Entities;
using ChurchWebsite.Core.Interfaces;
using FastEndpoints;

namespace ChurchWebsite.Api.Endpoints.Podcast;

public class PodcastEpisodeDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? SpeakerTitle { get; set; }
    public string SpeakerName { get; set; } = string.Empty;
    public string SpeakerDisplay { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Scripture { get; set; }
    public string? SeriesName { get; set; }
    public string AudioUrl { get; set; } = string.Empty;
    public string AudioFileName { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public long AudioFileSize { get; set; }
    public string AudioContentType { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TranscriptStatus { get; set; } = "none";
    public string? TranscriptUrl { get; set; }
    public string? TranscriptError { get; set; }
    public string SummaryStatus { get; set; } = "none";
    public string? SummaryError { get; set; }
    public List<string> Tags { get; set; } = [];
}

public static class PodcastEpisodeMapper
{
    public static PodcastEpisodeDto ToDto(
        PodcastEpisode episode,
        IFileStorageService fileStorage,
        IReadOnlyDictionary<string, string> titleAbbreviations,
        IConfiguration? configuration = null)
    {
        var variants = SpeakerFormatter.Format(episode.SpeakerTitle, episode.SpeakerName, titleAbbreviations);

        var coverImageUrl = ResolveCoverImageUrl(episode, fileStorage, configuration);

        return new PodcastEpisodeDto
        {
            Id = episode.Id,
            Title = episode.Title,
            SpeakerTitle = episode.SpeakerTitle,
            SpeakerName = episode.SpeakerName,
            SpeakerDisplay = variants.FullFormal,
            Description = episode.Description,
            Scripture = episode.Scripture,
            SeriesName = episode.SeriesName,
            AudioUrl = fileStorage.GetPublicUrl(episode.AudioFilePath),
            AudioFileName = episode.AudioFileName,
            CoverImageUrl = coverImageUrl,
            AudioFileSize = episode.AudioFileSize,
            AudioContentType = episode.AudioContentType,
            PublishedAt = episode.PublishedAt,
            CreatedAt = episode.CreatedAt,
            TranscriptStatus = episode.TranscriptStatus,
            TranscriptUrl = episode.TranscriptStatus == "completed" && !string.IsNullOrWhiteSpace(episode.TranscriptFilePath)
                ? fileStorage.GetTranscriptPublicUrl(episode.TranscriptFilePath)
                : null,
            TranscriptError = episode.TranscriptError,
            SummaryStatus = episode.SummaryStatus,
            SummaryError = episode.SummaryError,
            Tags = episode.Tags
        };
    }

    private static string ResolveCoverImageUrl(
        PodcastEpisode episode,
        IFileStorageService fileStorage,
        IConfiguration? configuration)
    {
        if (!string.IsNullOrWhiteSpace(episode.CoverImagePath))
        {
            return fileStorage.GetImagePublicUrl(episode.CoverImagePath);
        }

        var defaultImage = configuration?["Storage:DefaultCoverImage"];
        if (!string.IsNullOrWhiteSpace(defaultImage))
        {
            return fileStorage.GetImagePublicUrl(defaultImage);
        }

        return string.Empty;
    }

    /// <summary>
    /// Convenience for callers that have already bound an abbreviation dictionary
    /// from <c>IConfiguration</c> and want to reuse it across a list of episodes.
    /// </summary>
    public static IReadOnlyDictionary<string, string> LoadTitleAbbreviations(IConfiguration configuration)
    {
        var raw = configuration.GetSection("Speakers:TitleAbbreviations").Get<Dictionary<string, string>>()
            ?? new Dictionary<string, string>();
        return new Dictionary<string, string>(raw, StringComparer.OrdinalIgnoreCase);
    }
}
