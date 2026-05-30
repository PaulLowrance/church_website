using ChurchWebsite.Core.Interfaces;
using FastEndpoints;

namespace ChurchWebsite.Api.Endpoints.Podcast;

public class DeletePodcastEpisodeEndpoint(IPodcastEpisodeRepository repo, IFileStorageService fileStorage)
    : Endpoint<PodcastEpisodeRequest>
{
    public override void Configure()
    {
        Delete("/api/podcast/episodes/{id}");
        Roles("Admin");
    }

    public override async Task HandleAsync(PodcastEpisodeRequest req, CancellationToken ct)
    {
        var episode = await repo.GetByIdAsync(req.Id);
        if (episode is null)
        {
            await Send.NotFoundAsync(cancellation: ct);
            return;
        }

        await fileStorage.DeleteAudioFileAsync(episode.AudioFilePath, ct);
        await repo.DeleteAsync(req.Id);

        await Send.NoContentAsync(cancellation: ct);
    }
}
