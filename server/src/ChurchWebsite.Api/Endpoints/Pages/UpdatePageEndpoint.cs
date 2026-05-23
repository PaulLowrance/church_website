using ChurchWebsite.Core.Entities;
using ChurchWebsite.Core.Interfaces;
using FastEndpoints;

namespace ChurchWebsite.Api.Endpoints.Pages;

public class UpdatePageRequest
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsMarkdown { get; set; }
    public bool IsPublished { get; set; }
}

public class UpdatePageResponse
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsMarkdown { get; set; }
    public bool IsPublished { get; set; }
}

public class UpdatePageEndpoint(IPageRepository pageRepo) : Endpoint<UpdatePageRequest, UpdatePageResponse>
{
    public override void Configure()
    {
        Put("/api/pages/{slug}");
        Roles("Admin");
    }

    public override async Task HandleAsync(UpdatePageRequest req, CancellationToken ct)
    {
        var existing = await pageRepo.GetBySlugAsync(req.Slug);
        if (existing is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        existing.Title = req.Title;
        existing.Body = req.Body;
        existing.IsMarkdown = req.IsMarkdown;
        existing.IsPublished = req.IsPublished;
        existing.UpdatedAt = DateTime.UtcNow;

        await pageRepo.UpdateAsync(existing);

        await Send.OkAsync(new UpdatePageResponse
        {
            Slug = existing.Slug,
            Title = existing.Title,
            Body = existing.Body,
            IsMarkdown = existing.IsMarkdown,
            IsPublished = existing.IsPublished
        }, cancellation: ct);
    }
}
