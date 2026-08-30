using ChurchWebsite.Core;
using ChurchWebsite.Core.Interfaces;
using FastEndpoints;
using Microsoft.Extensions.Configuration;

namespace ChurchWebsite.Api.Endpoints.Podcast;

public class GetPodcastEpisodesRequest
{
    public string? Series { get; set; }
    public string? Speaker { get; set; }
    public string? Scripture { get; set; }
    public int? Year { get; set; }
    public string? Search { get; set; }

    public List<string> ParsedScriptures =>
        string.IsNullOrWhiteSpace(Scripture)
            ? []
            : [.. Scripture.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim().TrimStart('&').Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))];
}

public class GetPodcastEpisodesEndpoint(
    IPodcastEpisodeRepository repo,
    IFileStorageService fileStorage,
    IConfiguration configuration)
    : Endpoint<GetPodcastEpisodesRequest, List<PodcastEpisodeDto>>
{
    public override void Configure()
    {
        Get("/api/podcast/episodes");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetPodcastEpisodesRequest req, CancellationToken ct)
    {
        var filter = new PodcastEpisodeFilter
        {
            Series = req.Series,
            Speaker = req.Speaker,
            Scriptures = req.ParsedScriptures,
            Year = req.Year,
            Search = req.Search
        };

        var episodes = await repo.GetFilteredAsync(filter);
        var abbr = PodcastEpisodeMapper.LoadTitleAbbreviations(configuration);
        var dtos = episodes.Select(e => PodcastEpisodeMapper.ToDto(e, fileStorage, abbr, configuration)).ToList();
        await Send.OkAsync(dtos, cancellation: ct);
    }
}
