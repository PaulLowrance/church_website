using ChurchWebsite.Core.Interfaces;
using FastEndpoints;

namespace ChurchWebsite.Api.Endpoints.Pages;

public class DeletePageEndpoint(IPageRepository pageRepo) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/api/pages/{slug}");
        Roles("Admin");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var slug = Route<string>("slug") ?? string.Empty;
        var existing = await pageRepo.GetBySlugAsync(slug);
        if (existing is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await pageRepo.DeleteAsync(slug);
        await Send.NoContentAsync(ct);
    }
}
