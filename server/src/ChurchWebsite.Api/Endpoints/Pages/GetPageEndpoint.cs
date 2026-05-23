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
    public bool IsMarkdown { get; set; }
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

        await Send.OkAsync(new GetPageResponse
        {
            Title = page.Title,
            Body = page.Body,
            IsMarkdown = page.IsMarkdown
        }, cancellation: ct);
    }
}
