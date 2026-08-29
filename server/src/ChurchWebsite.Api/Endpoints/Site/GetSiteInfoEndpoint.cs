using FastEndpoints;

namespace ChurchWebsite.Api.Endpoints.Site;

public class SiteInfoResponse
{
    public string ChurchName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Telephone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string StreetAddress { get; set; } = string.Empty;
    public string AddressLocality { get; set; } = string.Empty;
    public string AddressRegion { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string AddressCountry { get; set; } = string.Empty;
    public double? GeoLatitude { get; set; }
    public double? GeoLongitude { get; set; }
    public string Denomination { get; set; } = string.Empty;
    public string DefaultCoverImage { get; set; } = string.Empty;
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
        var config = Config;
        var churchName = config["Site:ChurchName"] ?? "Brentwood Hills Primitive Baptist Church";
        var url = config["Site:Url"] ?? config["Podcast:BaseUrl"] ?? "https://bhpbc.org";

        double? ParseGeo(string? value) =>
            double.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : null;

        await Send.OkAsync(new SiteInfoResponse
        {
            ChurchName = churchName,
            Url = url.TrimEnd('/'),
            Telephone = config["Site:Telephone"] ?? string.Empty,
            Email = config["Site:Email"] ?? string.Empty,
            StreetAddress = config["Site:StreetAddress"] ?? string.Empty,
            AddressLocality = config["Site:AddressLocality"] ?? string.Empty,
            AddressRegion = config["Site:AddressRegion"] ?? string.Empty,
            PostalCode = config["Site:PostalCode"] ?? string.Empty,
            AddressCountry = config["Site:AddressCountry"] ?? "US",
            GeoLatitude = ParseGeo(config["Site:GeoLatitude"]),
            GeoLongitude = ParseGeo(config["Site:GeoLongitude"]),
            Denomination = config["Site:Denomination"] ?? "Primitive Baptist",
            DefaultCoverImage = config["Storage:DefaultCoverImage"] ?? string.Empty
        }, cancellation: ct);
    }
}