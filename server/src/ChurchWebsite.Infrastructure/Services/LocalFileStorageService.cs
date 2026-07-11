using ChurchWebsite.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ChurchWebsite.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _audioStoragePath;
    private readonly string _audioPublicPath;
    private readonly string _imagesStoragePath;
    private readonly string _imagesPublicPath;

    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".bmp"
    };

    public LocalFileStorageService(IConfiguration configuration)
    {
        _audioStoragePath = MakeAbsolutePath(configuration["Storage:AudioPath"] ?? "uploads/audio");
        _audioPublicPath = configuration["Storage:PublicPath"] ?? "/uploads/audio";

        _imagesStoragePath = MakeAbsolutePath(configuration["Storage:ImagesPath"] ?? "uploads/images");
        _imagesPublicPath = configuration["Storage:ImagesPublicPath"] ?? "/uploads/images";

        Directory.CreateDirectory(_audioStoragePath);
        Directory.CreateDirectory(_imagesStoragePath);
    }

    private static string MakeAbsolutePath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), path);
    }

    public async Task<string> SaveAudioFileAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        var uniqueFileName = MakeUniqueFileName(fileName);
        var filePath = Path.Combine(_audioStoragePath, uniqueFileName);

        await using var outputStream = File.Create(filePath);
        await fileStream.CopyToAsync(outputStream, ct);

        return filePath;
    }

    public Task DeleteAudioFileAsync(string filePath, CancellationToken ct = default)
    {
        DeleteFileIfExists(filePath);
        return Task.CompletedTask;
    }

    public string GetPublicUrl(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        return $"{_audioPublicPath.TrimEnd('/')}/{fileName}";
    }

    public async Task<string> SaveImageFileAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || !AllowedImageExtensions.Contains(extension))
        {
            throw new InvalidOperationException(
                $"Invalid image file extension '{extension}'. Allowed extensions: {string.Join(", ", AllowedImageExtensions)}");
        }

        var uniqueFileName = MakeUniqueFileName(fileName);
        var filePath = Path.Combine(_imagesStoragePath, uniqueFileName);

        await using var outputStream = File.Create(filePath);
        await fileStream.CopyToAsync(outputStream, ct);

        return filePath;
    }

    public Task DeleteImageFileAsync(string filePath, CancellationToken ct = default)
    {
        DeleteFileIfExists(filePath);
        return Task.CompletedTask;
    }

    public string GetImagePublicUrl(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        return $"{_imagesPublicPath.TrimEnd('/')}/{fileName}";
    }

    private static string MakeUniqueFileName(string fileName)
    {
        var safeFileName = Path.GetFileNameWithoutExtension(fileName)
            .Replace(" ", "_");
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            safeFileName = safeFileName.Replace(c.ToString(), "_");
        }
        var extension = Path.GetExtension(fileName);
        return $"{safeFileName}_{Guid.NewGuid():N}{extension}";
    }

    private static void DeleteFileIfExists(string filePath)
    {
        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}
