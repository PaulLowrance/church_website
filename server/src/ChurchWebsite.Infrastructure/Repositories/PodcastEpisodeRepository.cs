using System.Data;
using System.Text.RegularExpressions;
using Dapper;
using ChurchWebsite.Core.Entities;
using ChurchWebsite.Core.Interfaces;
using ChurchWebsite.Infrastructure.Data;

namespace ChurchWebsite.Infrastructure.Repositories;

public class PodcastEpisodeRepository(DbConnectionFactory factory) : IPodcastEpisodeRepository
{
    public async Task<PodcastEpisode?> GetByIdAsync(Guid id)
    {
        using var conn = factory.CreateConnection();
        var episode = await conn.QueryFirstOrDefaultAsync<PodcastEpisode>(
            "SELECT * FROM podcast_episodes WHERE id = @id",
            new { id });

        if (episode is not null)
        {
            episode.Tags = (await GetTagsAsync(id)).ToList();
        }

        return episode;
    }

    public async Task<IEnumerable<PodcastEpisode>> GetAllAsync()
    {
        using var conn = factory.CreateConnection();
        var episodes = await conn.QueryAsync<PodcastEpisode>(
            "SELECT * FROM podcast_episodes ORDER BY published_at DESC");

        foreach (var episode in episodes)
        {
            episode.Tags = (await GetTagsAsync(episode.Id)).ToList();
        }

        return episodes;
    }

    public async Task<IEnumerable<PodcastEpisode>> GetPublishedAsync()
    {
        using var conn = factory.CreateConnection();
        var episodes = await conn.QueryAsync<PodcastEpisode>(
            "SELECT * FROM podcast_episodes WHERE published_at <= @now ORDER BY published_at DESC",
            new { now = DateTime.UtcNow });

        foreach (var episode in episodes)
        {
            episode.Tags = (await GetTagsAsync(episode.Id)).ToList();
        }

        return episodes;
    }

    public async Task<IEnumerable<PodcastEpisode>> GetByTranscriptStatusAsync(string status)
    {
        using var conn = factory.CreateConnection();
        var episodes = await conn.QueryAsync<PodcastEpisode>(
            "SELECT * FROM podcast_episodes WHERE transcript_status = @status ORDER BY created_at ASC",
            new { status });

        foreach (var episode in episodes)
        {
            episode.Tags = (await GetTagsAsync(episode.Id)).ToList();
        }

        return episodes;
    }

    public async Task CreateAsync(PodcastEpisode episode)
    {
        using var conn = factory.CreateConnection();
        await conn.ExecuteAsync(
            @"INSERT INTO podcast_episodes (id, title, speaker_title, speaker_name, description, series_name, audio_file_path, audio_file_name, audio_file_size, audio_content_type, published_at, created_at, updated_at, transcript_status, transcript_file_path, assemblyai_transcript_id, transcript_error, summary_status, summary_error)
              VALUES (@Id, @Title, @SpeakerTitle, @SpeakerName, @Description, @SeriesName, @AudioFilePath, @AudioFileName, @AudioFileSize, @AudioContentType, @PublishedAt, @CreatedAt, @UpdatedAt, @TranscriptStatus, @TranscriptFilePath, @AssemblyAiTranscriptId, @TranscriptError, @SummaryStatus, @SummaryError)",
            episode);

        await SetTagsAsync(episode.Id, episode.Tags);
    }

    public async Task UpdateAsync(PodcastEpisode episode)
    {
        using var conn = factory.CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE podcast_episodes SET title = @Title, speaker_title = @SpeakerTitle, speaker_name = @SpeakerName, description = @Description, series_name = @SeriesName, audio_file_path = @AudioFilePath, audio_file_name = @AudioFileName, audio_file_size = @AudioFileSize, audio_content_type = @AudioContentType, published_at = @PublishedAt, updated_at = @UpdatedAt, transcript_status = @TranscriptStatus, transcript_file_path = @TranscriptFilePath, assemblyai_transcript_id = @AssemblyAiTranscriptId, transcript_error = @TranscriptError, summary_status = @SummaryStatus, summary_error = @SummaryError WHERE id = @Id",
            episode);

        await SetTagsAsync(episode.Id, episode.Tags);
    }

    public async Task DeleteAsync(Guid id)
    {
        using var conn = factory.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM episode_tags WHERE episode_id = @id", new { id });
        await conn.ExecuteAsync("DELETE FROM podcast_episodes WHERE id = @id", new { id });
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        using var conn = factory.CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM podcast_episodes WHERE id = @id",
            new { id });
        return count > 0;
    }

    public async Task<IEnumerable<string>> GetTagsAsync(Guid episodeId)
    {
        using var conn = factory.CreateConnection();
        return await conn.QueryAsync<string>(
            "SELECT tag FROM episode_tags WHERE episode_id = @episodeId ORDER BY tag",
            new { episodeId });
    }

    public async Task SetTagsAsync(Guid episodeId, IEnumerable<string> tags)
    {
        using var conn = factory.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM episode_tags WHERE episode_id = @episodeId", new { episodeId });

        var uniqueTags = tags
            .Select(t => Regex.Replace(t.ToLowerInvariant().Trim(), @"\s+", " "))
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct();

        foreach (var tag in uniqueTags)
        {
            await conn.ExecuteAsync(
                "INSERT INTO episode_tags (episode_id, tag) VALUES (@episodeId, @tag)",
                new { episodeId, tag });
        }
    }
}
