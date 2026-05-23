using ChurchWebsite.Core.Entities;
using ChurchWebsite.Core.Interfaces;
using FastEndpoints;

namespace ChurchWebsite.Api.Endpoints.Pages;

public class PageListItem
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsMarkdown { get; set; }
}

public class GetAllPagesEndpoint(IPageRepository pageRepo) : EndpointWithoutRequest<IEnumerable<PageListItem>>
{
    public override void Configure()
    {
        Get("/api/pages");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var pages = await pageRepo.GetAllAsync();
        var result = pages.Select(p => new PageListItem
        {
            Slug = p.Slug,
            Title = p.Title,
            IsMarkdown = p.IsMarkdown
        });
        await Send.OkAsync(result, cancellation: ct);
    }
}
