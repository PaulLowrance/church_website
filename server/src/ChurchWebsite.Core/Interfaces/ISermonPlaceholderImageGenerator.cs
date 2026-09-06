using ChurchWebsite.Core.Entities;

namespace ChurchWebsite.Core.Interfaces;

public interface ISermonPlaceholderImageGenerator
{
    Task<string> GenerateAsync(PodcastEpisode episode, CancellationToken ct = default);
    bool IsPlaceholderImage(string? filePath);
}
