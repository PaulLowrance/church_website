using ChurchWebsite.Core.Interfaces;
using FastEndpoints;

namespace ChurchWebsite.Api.Endpoints.Podcast;

public class GetPodcastEpisodeEndpoint(IPodcastEpisodeRepository repo, IFileStorageService fileStorage)
    : Endpoint<PodcastEpisodeRequest, PodcastEpisodeDto>
{
    public override void Configure()
    {
        Get("/api/podcast/episodes/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(PodcastEpisodeRequest req, CancellationToken ct)
    {
        var episode = await repo.GetByIdAsync(req.Id);
        if (episode is null || episode.PublishedAt > DateTime.UtcNow)
        {
            await Send.NotFoundAsync(cancellation: ct);
            return;
        }

        await Send.OkAsync(PodcastEpisodeMapper.ToDto(episode, fileStorage), cancellation: ct);
    }
}

public class PodcastEpisodeRequest
{
    public Guid Id { get; set; }
}
