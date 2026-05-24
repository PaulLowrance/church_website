using ChurchWebsite.Core.Entities;
using ChurchWebsite.Core.Interfaces;
using FastEndpoints;

namespace ChurchWebsite.Api.Endpoints.Pages;

public class PageListItem
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsMarkdown { get; set; }
    public bool IsPublished { get; set; }
    public bool ShowInNav { get; set; }
    public string NavTitle { get; set; } = string.Empty;
}

public class GetAllPagesEndpoint(IPageRepository pageRepo) : EndpointWithoutRequest<IEnumerable<PageListItem>>
{
    public override void Configure()
    {
        Get("/api/pages");
        Roles("Admin");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var pages = await pageRepo.GetAllAsync();
        var result = pages.Select(p => new PageListItem
        {
            Slug = p.Slug,
            Title = p.Title,
            IsMarkdown = p.IsMarkdown,
            IsPublished = p.IsPublished,
            ShowInNav = p.ShowInNav,
            NavTitle = p.NavTitle
        });
        await Send.OkAsync(result, cancellation: ct);
    }
}
