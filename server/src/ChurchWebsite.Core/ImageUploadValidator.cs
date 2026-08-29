namespace ChurchWebsite.Core;

public class ImageUploadOptions
{
    public int MaxImageBytes { get; set; } = 10 * 1024 * 1024;
}

public static class ImageUploadValidator
{
    public static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".bmp"
    };

    public static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp", "image/svg+xml", "image/bmp"
    };

    public static (bool IsValid, string? Error) Validate(string? fileName, string? contentType, long length, ImageUploadOptions options)
    {
        if (string.IsNullOrWhiteSpace(fileName) || length <= 0)
        {
            return (false, "Image file is required.");
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            return (false,
                $"Invalid image file extension '{extension}'. Allowed: {string.Join(", ", AllowedExtensions)}.");
        }

        var ct = contentType ?? string.Empty;
        if (string.IsNullOrWhiteSpace(ct) || !AllowedContentTypes.Contains(ct))
        {
            return (false,
                $"Image content type '{ct}' is not allowed. Allowed: image/jpeg, image/png, image/gif, image/webp, image/svg+xml, image/bmp.");
        }

        if (length > options.MaxImageBytes)
        {
            var actualMb = length / (1024 * 1024);
            var maxMb = options.MaxImageBytes / (1024 * 1024);
            return (false,
                $"Image file is {actualMb} MB which exceeds the {maxMb} MB maximum.");
        }

        return (true, null);
    }

    public static int ResolveMaxImageBytes(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        var raw = configuration["Images:MaxImageBytes"];
        return int.TryParse(raw, out var b) && b > 0 ? b : 10 * 1024 * 1024;
    }

    public static ImageUploadOptions BuildOptions(Microsoft.Extensions.Configuration.IConfiguration configuration) =>
        new() { MaxImageBytes = ResolveMaxImageBytes(configuration) };
}
