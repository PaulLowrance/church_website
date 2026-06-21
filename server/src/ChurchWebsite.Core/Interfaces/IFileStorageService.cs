namespace ChurchWebsite.Core.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveAudioFileAsync(Stream fileStream, string fileName, CancellationToken ct = default);
    Task DeleteAudioFileAsync(string filePath, CancellationToken ct = default);
    string GetPublicUrl(string filePath);
}
