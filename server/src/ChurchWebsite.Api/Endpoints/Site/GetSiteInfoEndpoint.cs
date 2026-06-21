using FastEndpoints;

namespace ChurchWebsite.Api.Endpoints.Site;

public class SiteInfoResponse
{
    public string ChurchName { get; set; } = string.Empty;
}

public class GetSiteInfoEndpoint : EndpointWithoutRequest<SiteInfoResponse>
{
    public override void Configure()
    {
        Get("/api/site-info");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var churchName = Config["Site:ChurchName"] ?? "Brentwood Hills Primitive Baptist Church";

        await Send.OkAsync(new SiteInfoResponse
        {
            ChurchName = churchName
        }, cancellation: ct);
    }
}
