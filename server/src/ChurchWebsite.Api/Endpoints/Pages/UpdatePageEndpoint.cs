using ChurchWebsite.Core.Entities;
using ChurchWebsite.Core.Interfaces;
using FastEndpoints;

namespace ChurchWebsite.Api.Endpoints.Pages;

public class UpdatePageRequest
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string ContentType { get; set; } = "wysiwyg";
    public bool IsPublished { get; set; }
    public bool ShowInNav { get; set; }
    public string NavTitle { get; set; } = string.Empty;
}

public class UpdatePageResponse
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string ContentType { get; set; } = "wysiwyg";
    public bool IsPublished { get; set; }
    public bool ShowInNav { get; set; }
    public string NavTitle { get; set; } = string.Empty;
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

        var navTitle = string.IsNullOrWhiteSpace(req.NavTitle) ? req.Title.Trim() : req.NavTitle.Trim();
        if (navTitle.Length > 25)
            navTitle = navTitle[..25];

        existing.Title = req.Title;
        existing.Body = req.Body;
        existing.ContentType = req.ContentType;
        existing.IsPublished = req.IsPublished;
        existing.ShowInNav = req.ShowInNav;
        existing.NavTitle = navTitle;
        existing.UpdatedAt = DateTime.UtcNow;

        await pageRepo.UpdateAsync(existing);

        await Send.OkAsync(new UpdatePageResponse
        {
            Slug = existing.Slug,
            Title = existing.Title,
            Body = existing.Body,
            ContentType = existing.ContentType,
            IsPublished = existing.IsPublished,
            ShowInNav = existing.ShowInNav,
            NavTitle = existing.NavTitle
        }, cancellation: ct);
    }
}
