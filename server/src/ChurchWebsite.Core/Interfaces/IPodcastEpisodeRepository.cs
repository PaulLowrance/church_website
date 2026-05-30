using ChurchWebsite.Core.Entities;

namespace ChurchWebsite.Core.Interfaces;

public interface IPodcastEpisodeRepository
{
    Task<PodcastEpisode?> GetByIdAsync(Guid id);
    Task<IEnumerable<PodcastEpisode>> GetAllAsync();
    Task<IEnumerable<PodcastEpisode>> GetPublishedAsync();
    Task CreateAsync(PodcastEpisode episode);
    Task UpdateAsync(PodcastEpisode episode);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<IEnumerable<string>> GetTagsAsync(Guid episodeId);
    Task SetTagsAsync(Guid episodeId, IEnumerable<string> tags);
}
