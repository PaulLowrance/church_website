using ChurchWebsite.Core.Interfaces;
using FastEndpoints;

namespace ChurchWebsite.Api.Endpoints.Images;

public class UploadImageRequest
{
    public IFormFile ImageFile { get; set; } = null!;
}

public class UploadImageResponse
{
    public string PublicUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}

public class UploadImageEndpoint(IFileStorageService fileStorage) : Endpoint<UploadImageRequest, UploadImageResponse>
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp", "image/svg+xml", "image/bmp"
    };

    public override void Configure()
    {
        Post("/api/images");
        Roles("Admin");
        AllowFileUploads();
    }

    public override async Task HandleAsync(UploadImageRequest req, CancellationToken ct)
    {
        if (req.ImageFile is null || req.ImageFile.Length == 0)
        {
            ThrowError("Image file is required");
            return;
        }

        if (!AllowedContentTypes.Contains(req.ImageFile.ContentType ?? string.Empty))
        {
            ThrowError($"Unsupported image type '{req.ImageFile.ContentType}'. Please upload a JPEG, PNG, GIF, WebP, SVG, or BMP image.");
            return;
        }

        var filePath = await fileStorage.SaveImageFileAsync(req.ImageFile.OpenReadStream(), req.ImageFile.FileName, ct);

        await Send.OkAsync(new UploadImageResponse
        {
            PublicUrl = fileStorage.GetImagePublicUrl(filePath),
            FileName = Path.GetFileName(filePath)
        }, cancellation: ct);
    }
}
