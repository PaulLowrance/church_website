using ChurchWebsite.Core.Interfaces;
using FastEndpoints;
using Microsoft.Extensions.Configuration;

namespace ChurchWebsite.Api.Endpoints.Podcast;

public class GetAllPodcastEpisodesEndpoint(
    IPodcastEpisodeRepository repo,
    IFileStorageService fileStorage,
    IConfiguration configuration)
    : EndpointWithoutRequest<List<PodcastEpisodeDto>>
{
    public override void Configure()
    {
        Get("/api/admin/podcast/episodes");
        Roles("Admin");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var episodes = await repo.GetAllAsync();
        var abbr = PodcastEpisodeMapper.LoadTitleAbbreviations(configuration);
        var dtos = episodes.Select(e => PodcastEpisodeMapper.ToDto(e, fileStorage, abbr, configuration)).ToList();
        await Send.OkAsync(dtos, cancellation: ct);
    }
}
