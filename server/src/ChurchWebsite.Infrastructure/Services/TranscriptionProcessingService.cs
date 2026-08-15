using System.Text;
using ChurchWebsite.Core.Entities;
using ChurchWebsite.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChurchWebsite.Infrastructure.Services;

public class TranscriptionProcessingService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<TranscriptionProcessingService> logger) : BackgroundService
{
    private static readonly string[] InProgressStatuses = ["queued", "processing"];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollInterval = TimeSpan.FromSeconds(
            int.TryParse(configuration["AssemblyAI:PollIntervalSeconds"], out var seconds) && seconds > 0
                ? seconds
                : 15);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueEpisodesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transcription processing loop failed for this iteration.");
            }

            await Task.Delay(pollInterval, stoppingToken);
        }
    }

    private async Task ProcessDueEpisodesAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IPodcastEpisodeRepository>();
        var transcription = scope.ServiceProvider.GetRequiredService<ITranscriptionService>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();

        foreach (var status in InProgressStatuses)
        {
            var episodes = await repo.GetByTranscriptStatusAsync(status);
            foreach (var episode in episodes)
            {
                await ProcessEpisodeAsync(episode, repo, transcription, fileStorage, ct);
            }
        }
    }

    private async Task ProcessEpisodeAsync(
        PodcastEpisode episode,
        IPodcastEpisodeRepository repo,
        ITranscriptionService transcription,
        IFileStorageService fileStorage,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(episode.AssemblyAiTranscriptId))
        {
            return;
        }

        var result = await transcription.GetResultAsync(episode.AssemblyAiTranscriptId, ct);

        switch (result.Status)
        {
            case "queued":
            case "processing":
                return;
            case "error":
                episode.TranscriptStatus = "error";
                episode.TranscriptError = result.Error ?? "AssemblyAI transcription failed.";
                episode.UpdatedAt = DateTime.UtcNow;
                await repo.UpdateAsync(episode);
                logger.LogWarning("Transcription failed for episode {EpisodeId}: {Error}", episode.Id, episode.TranscriptError);
                return;
            case "completed":
                await CompleteAsync(episode, result.Text, repo, transcription, fileStorage, ct);
                return;
        }
    }

    private async Task CompleteAsync(
        PodcastEpisode episode,
        string? text,
        IPodcastEpisodeRepository repo,
        ITranscriptionService transcription,
        IFileStorageService fileStorage,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            episode.TranscriptStatus = "error";
            episode.TranscriptError = "AssemblyAI completed but returned no transcript text.";
            episode.UpdatedAt = DateTime.UtcNow;
            await repo.UpdateAsync(episode);
            return;
        }

        var fileName = $"{Sanitize(episode.Title)}_{episode.Id:N}.txt";
        await using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
        {
            episode.TranscriptFilePath = await fileStorage.SaveTranscriptFileAsync(stream, fileName, ct);
        }

        episode.TranscriptStatus = "completed";
        episode.TranscriptError = null;
        episode.UpdatedAt = DateTime.UtcNow;
        await repo.UpdateAsync(episode);

        await GenerateSummaryAsync(episode, text, repo, transcription, ct);
    }

    private async Task GenerateSummaryAsync(
        PodcastEpisode episode,
        string text,
        IPodcastEpisodeRepository repo,
        ITranscriptionService transcription,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(episode.Description))
        {
            return;
        }

        episode.SummaryStatus = "processing";
        episode.SummaryError = null;
        episode.UpdatedAt = DateTime.UtcNow;
        await repo.UpdateAsync(episode);

        try
        {
            var summary = await transcription.SummarizeAsync(text, ct);
            if (!string.IsNullOrWhiteSpace(summary))
            {
                episode.Description = summary;
                episode.SummaryStatus = "completed";
                episode.SummaryError = null;
            }
            else
            {
                episode.SummaryStatus = "error";
                episode.SummaryError = "LLM Gateway returned an empty summary.";
            }
        }
        catch (Exception ex)
        {
            episode.SummaryStatus = "error";
            episode.SummaryError = ex.Message;
            logger.LogError(ex, "Summary generation failed for episode {EpisodeId}", episode.Id);
        }

        episode.UpdatedAt = DateTime.UtcNow;
        await repo.UpdateAsync(episode);
    }

    private static string Sanitize(string value)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(c.ToString(), "_");
        }
        return string.IsNullOrWhiteSpace(value) ? "transcript" : value.Replace(" ", "_");
    }
}
