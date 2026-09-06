using System.Text;
using System.Text.RegularExpressions;
using ChurchWebsite.Core.Entities;
using ChurchWebsite.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ChurchWebsite.Infrastructure.Services;

public partial class SermonPlaceholderImageGenerator : ISermonPlaceholderImageGenerator
{
    private readonly IFileStorageService _fileStorage;
    private readonly string _churchName;

    private const string PlaceholderFilePrefix = "sermon-placeholder-";
    private const int SvgWidth = 1024;
    private const int SvgHeight = 1024;
    private const int TitleMaxCharsPerLine = 42;
    private const int TitleMaxLines = 3;
    private const int TitleLineHeight = 58;
    private const int TitleStartY = 460;

    public SermonPlaceholderImageGenerator(IFileStorageService fileStorage, IConfiguration configuration)
    {
        _fileStorage = fileStorage;
        _churchName = configuration["Site:ChurchName"] ?? "Brentwood Hills Primitive Baptist Church";
    }

    public async Task<string> GenerateAsync(PodcastEpisode episode, CancellationToken ct = default)
    {
        var existingPlaceholder = episode.CoverImagePath;
        if (IsPlaceholderImage(existingPlaceholder))
        {
            await _fileStorage.DeleteImageFileAsync(existingPlaceholder!, ct);
        }

        var dateText = episode.PublishedAt.ToString("MMMM d, yyyy");
        var speakerText = FormatSpeaker(episode);
        var titleLines = WrapText(EscapeXml(episode.Title), TitleMaxCharsPerLine, TitleMaxLines);

        var svg = BuildSvg(titleLines, speakerText, dateText);
        var fileName = $"{PlaceholderFilePrefix}{episode.Id:N}.svg";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(svg));
        return await _fileStorage.SaveImageFileAsync(stream, fileName, ct);
    }

    public bool IsPlaceholderImage(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        var fileName = Path.GetFileName(filePath);
        return fileName.StartsWith(PlaceholderFilePrefix, StringComparison.OrdinalIgnoreCase)
            && fileName.EndsWith(".svg", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatSpeaker(PodcastEpisode episode)
    {
        if (!string.IsNullOrWhiteSpace(episode.SpeakerTitle))
        {
            return $"{episode.SpeakerTitle} {episode.SpeakerName}";
        }

        return episode.SpeakerName;
    }

    private string BuildSvg(IReadOnlyList<string> titleLines, string speaker, string date)
    {
        var titleY = TitleStartY - ((titleLines.Count - 1) * TitleLineHeight / 2);
        var titleGroup = new StringBuilder();
        for (var i = 0; i < titleLines.Count; i++)
        {
            var y = titleY + (i * TitleLineHeight);
            titleGroup.AppendLine($"""    <tspan x="512" y="{y}">{titleLines[i]}</tspan>""");
        }

        var speakerDate = $"{EscapeXml(speaker)}  •  {date}";

        return $"""
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {SvgWidth} {SvgHeight}" width="{SvgWidth}" height="{SvgHeight}">
              <rect width="{SvgWidth}" height="{SvgHeight}" fill="#fbf8f3"/>
              <rect x="32" y="32" width="960" height="960" fill="none" stroke="#6f2e2a" stroke-width="8" opacity="0.3"/>
              <rect x="48" y="48" width="928" height="928" fill="none" stroke="#6f2e2a" stroke-width="2" opacity="0.2"/>

              <g transform="translate(362, 150)">
                <path d="M 20,180 Q 150,150 280,180 L 280,210 Q 150,180 20,210 Z" fill="#b08a7c" opacity="0.85"/>
                <path d="M 150,162 L 150,200" stroke="#fbf8f3" stroke-width="3" fill="none"/>
                <path d="M 30,140 Q 80,95 130,140 C 140,160 20,160 30,140 Z" fill="#b08a7c"/>
                <path d="M 55,128 Q 65,118 75,132" stroke="#fbf8f3" stroke-width="3" fill="none"/>
                <path d="M 85,122 Q 95,112 105,128" stroke="#fbf8f3" stroke-width="3" fill="none"/>
                <path d="M 160,40 L 240,40 C 240,90 215,110 200,110 C 185,110 160,90 160,40 Z" fill="#6f2e2a"/>
                <ellipse cx="200" cy="40" rx="40" ry="8" fill="#b08a7c"/>
                <rect x="195" y="110" width="10" height="40" fill="#6f2e2a"/>
                <circle cx="200" cy="128" r="9" fill="#6f2e2a"/>
                <path d="M 170,162 L 230,162 L 220,150 L 180,150 Z" fill="#6f2e2a"/>
              </g>

              <text font-family="Georgia, serif" font-size="48" font-weight="bold" fill="#6f2e2a" text-anchor="middle">
            {titleGroup}
              </text>

              <text x="512" y="650" font-family="'Helvetica Neue', Arial, sans-serif" font-size="28" fill="#b08a7c" text-anchor="middle">{speakerDate}</text>

              <text x="512" y="820" font-family="Georgia, serif" font-size="20" letter-spacing="2" fill="#1c1a17" text-anchor="middle">{EscapeXml(_churchName.ToUpperInvariant())}</text>
            </svg>
            """;
    }

    private static List<string> WrapText(string text, int maxCharsPerLine, int maxLines)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var currentLine = new StringBuilder();
        var truncated = false;

        foreach (var word in words)
        {
            if (lines.Count >= maxLines)
            {
                truncated = true;
                break;
            }

            var wouldOverflow = currentLine.Length > 0
                ? currentLine.Length + 1 + word.Length > maxCharsPerLine
                : word.Length > maxCharsPerLine;

            if (wouldOverflow && currentLine.Length > 0)
            {
                lines.Add(currentLine.ToString().TrimEnd());
                currentLine.Clear();

                if (lines.Count >= maxLines)
                {
                    truncated = true;
                    break;
                }
            }

            if (currentLine.Length > 0)
            {
                currentLine.Append(' ');
            }

            currentLine.Append(word);
        }

        if (currentLine.Length > 0)
        {
            if (lines.Count < maxLines)
            {
                lines.Add(currentLine.ToString().TrimEnd());
            }
            else
            {
                truncated = true;
            }
        }

        if (truncated && lines.Count > 0)
        {
            var lastLine = lines[^1];
            if (lastLine.Length > 3)
            {
                lines[^1] = lastLine[..^3] + "…";
            }
            else
            {
                lines[^1] = lastLine + "…";
            }
        }

        if (lines.Count == 0)
        {
            lines.Add(text);
        }

        return lines;
    }

    private static string EscapeXml(string input)
    {
        return input
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }
}
