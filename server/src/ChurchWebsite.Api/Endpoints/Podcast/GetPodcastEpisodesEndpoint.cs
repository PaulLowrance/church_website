using ChurchWebsite.Core.Interfaces;
using FastEndpoints;
using Microsoft.Extensions.Configuration;

namespace ChurchWebsite.Api.Endpoints.Podcast;

public class GetPodcastEpisodesEndpoint(
    IPodcastEpisodeRepository repo,
    IFileStorageService fileStorage,
    IConfiguration configuration)
    : EndpointWithoutRequest<List<PodcastEpisodeDto>>
{
    public override void Configure()
    {
        Get("/api/podcast/episodes");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var episodes = await repo.GetPublishedAsync();
        var abbr = PodcastEpisodeMapper.LoadTitleAbbreviations(configuration);
        var dtos = episodes.Select(e => PodcastEpisodeMapper.ToDto(e, fileStorage, abbr)).ToList();
        await Send.OkAsync(dtos, cancellation: ct);
    }
}
