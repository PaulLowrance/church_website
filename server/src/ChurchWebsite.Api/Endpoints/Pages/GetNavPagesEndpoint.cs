using ChurchWebsite.Core.Interfaces;
using FastEndpoints;

namespace ChurchWebsite.Api.Endpoints.Pages;

public class NavPageItem
{
    public string Slug { get; set; } = string.Empty;
    public string NavTitle { get; set; } = string.Empty;
}

public class GetNavPagesEndpoint(IPageRepository pageRepo) : EndpointWithoutRequest<IEnumerable<NavPageItem>>
{
    public override void Configure()
    {
        Get("/api/pages/nav");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var pages = await pageRepo.GetNavPagesAsync();
        var result = pages.Select(p => new NavPageItem
        {
            Slug = p.Slug,
            NavTitle = p.NavTitle
        });
        await Send.OkAsync(result, cancellation: ct);
    }
}
