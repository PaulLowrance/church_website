namespace ChurchWebsite.Core;

public class PodcastEpisodeFilter
{
    public string? Series { get; set; }
    public string? Speaker { get; set; }
    public List<string> Scriptures { get; set; } = [];
    public int? Year { get; set; }
    public string? Search { get; set; }
}
