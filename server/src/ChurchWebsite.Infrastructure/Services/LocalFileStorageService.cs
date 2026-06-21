using ChurchWebsite.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ChurchWebsite.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _storagePath;
    private readonly string _publicPath;

    public LocalFileStorageService(IConfiguration configuration)
    {
        _storagePath = configuration["Storage:AudioPath"] ?? "uploads/audio";
        if (!Path.IsPathRooted(_storagePath))
        {
            _storagePath = Path.Combine(Directory.GetCurrentDirectory(), _storagePath);
        }

        _publicPath = configuration["Storage:PublicPath"] ?? "/uploads/audio";

        Directory.CreateDirectory(_storagePath);
    }

    public async Task<string> SaveAudioFileAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        var safeFileName = Path.GetFileNameWithoutExtension(fileName)
            .Replace(" ", "_");
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            safeFileName = safeFileName.Replace(c.ToString(), "_");
        }
        var extension = Path.GetExtension(fileName);
        var uniqueFileName = $"{safeFileName}_{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(_storagePath, uniqueFileName);

        await using var outputStream = File.Create(filePath);
        await fileStream.CopyToAsync(outputStream, ct);

        return filePath;
    }

    public Task DeleteAudioFileAsync(string filePath, CancellationToken ct = default)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        return Task.CompletedTask;
    }

    public string GetPublicUrl(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        return $"{_publicPath.TrimEnd('/')}/{fileName}";
    }
}
