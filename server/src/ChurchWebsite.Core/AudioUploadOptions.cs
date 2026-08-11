namespace ChurchWebsite.Core;

public class AudioUploadOptions
{
    public int MaxAudioBytes { get; set; } = 524_288_000;
}

public static class AudioUploadValidator
{
    public static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".m4a", ".ogg"
    };

    public static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "audio/mpeg", "audio/mpeg3", "audio/x-mpeg-3",
        "audio/wav", "audio/wave", "audio/x-wav",
        "audio/mp4", "audio/x-m4a", "audio/aac",
        "audio/ogg"
    };

    public static (bool IsValid, string? Error) Validate(string? fileName, string? contentType, long length, AudioUploadOptions options)
    {
        if (string.IsNullOrWhiteSpace(fileName) || length <= 0)
        {
            return (false, "Audio file is required.");
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            return (false,
                $"Invalid audio file extension '{extension}'. Allowed: {string.Join(", ", AllowedExtensions)}.");
        }

        var ct = contentType ?? string.Empty;
        if (string.IsNullOrWhiteSpace(ct) || !AllowedContentTypes.Contains(ct))
        {
            return (false,
                $"Audio content type '{ct}' is not allowed. Allowed: audio/mpeg, audio/wav, audio/mp4, audio/ogg.");
        }

        if (length > options.MaxAudioBytes)
        {
            var actualMb = length / (1024 * 1024);
            var maxMb = options.MaxAudioBytes / (1024 * 1024);
            return (false,
                $"Audio file is {actualMb} MB which exceeds the {maxMb} MB maximum.");
        }

        return (true, null);
    }

    public static int ResolveMaxAudioBytes(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        var raw = configuration["Podcast:MaxAudioBytes"];
        return int.TryParse(raw, out var b) && b > 0 ? b : 524_288_000;
    }

    public static AudioUploadOptions BuildOptions(Microsoft.Extensions.Configuration.IConfiguration configuration) =>
        new() { MaxAudioBytes = ResolveMaxAudioBytes(configuration) };
}
