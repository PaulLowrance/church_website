using ChurchWebsite.Core.Interfaces;
using FastEndpoints;

namespace ChurchWebsite.Api.Endpoints.Podcast;

public class GetAllPodcastEpisodesEndpoint(IPodcastEpisodeRepository repo, IFileStorageService fileStorage)
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
        var dtos = episodes.Select(e => PodcastEpisodeMapper.ToDto(e, fileStorage)).ToList();
        await Send.OkAsync(dtos, cancellation: ct);
    }
}
