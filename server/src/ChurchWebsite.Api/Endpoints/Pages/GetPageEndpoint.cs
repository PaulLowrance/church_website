using ChurchWebsite.Core.Interfaces;
using FastEndpoints;

namespace ChurchWebsite.Api.Endpoints.Pages;

public class GetPageRequest
{
    public string Slug { get; set; } = string.Empty;
}

public class GetPageResponse
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string ContentType { get; set; } = "wysiwyg";
    public bool IsPublished { get; set; }
}

public class GetPageEndpoint(IPageRepository pageRepo) : Endpoint<GetPageRequest, GetPageResponse>
{
    public override void Configure()
    {
        Get("/api/pages/{slug}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetPageRequest req, CancellationToken ct)
    {
        var page = await pageRepo.GetBySlugAsync(req.Slug);
        if (page is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Unpublished pages are hidden from anonymous users
        if (!page.IsPublished && User.Identity?.IsAuthenticated != true)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(new GetPageResponse
        {
            Title = page.Title,
            Body = page.Body,
            ContentType = page.ContentType,
            IsPublished = page.IsPublished
        }, cancellation: ct);
    }
}
