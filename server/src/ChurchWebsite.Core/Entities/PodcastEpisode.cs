namespace ChurchWebsite.Core.Entities;

public class PodcastEpisode
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string SpeakerName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SeriesName { get; set; }
    public string AudioFilePath { get; set; } = string.Empty;
    public string AudioFileName { get; set; } = string.Empty;
    public long AudioFileSize { get; set; }
    public string AudioContentType { get; set; } = "audio/mpeg";
    public DateTime PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string TranscriptStatus { get; set; } = "none";
    public string? TranscriptFilePath { get; set; }
    public string? AssemblyAiTranscriptId { get; set; }
    public string? TranscriptError { get; set; }
    public string SummaryStatus { get; set; } = "none";
    public string? SummaryError { get; set; }
    public List<string> Tags { get; set; } = [];
}
