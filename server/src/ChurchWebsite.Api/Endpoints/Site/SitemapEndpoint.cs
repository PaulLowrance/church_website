using System.Text;
using System.Xml.Linq;
using ChurchWebsite.Core.Interfaces;
using FastEndpoints;
using Microsoft.Extensions.Caching.Memory;

namespace ChurchWebsite.Api.Endpoints.Site;

public class SitemapEndpoint(
    IPageRepository pageRepo,
    IPodcastEpisodeRepository episodeRepo,
    IConfiguration configuration,
    IMemoryCache cache) : EndpointWithoutRequest<string>
{
    public override void Configure()
    {
        Get("/sitemap.xml");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        HttpContext.Response.ContentType = "application/xml; charset=utf-8";

        const string cacheKey = "sitemap.xml";
        if (cache.TryGetValue(cacheKey, out string? cached) && cached is not null)
        {
            await Send.StringAsync(cached, cancellation: ct);
            return;
        }

        var baseUrl = configuration["Site:Url"]
            ?? configuration["Podcast:BaseUrl"]
            ?? "https://bhpbc.org";
        baseUrl = baseUrl.TrimEnd('/');

        var urls = new List<(string Loc, DateTime? LastMod)>();

        urls.Add(($"{baseUrl}/", null));

        foreach (var page in await pageRepo.GetPublishedPagesAsync())
        {
            urls.Add(($"{baseUrl}/{page.Slug}", page.UpdatedAt));
        }

        foreach (var episode in await episodeRepo.GetPublishedAsync())
        {
            urls.Add(($"{baseUrl}/sermon/{episode.Id}", episode.UpdatedAt));
        }

        var ns = (XNamespace)"http://www.sitemaps.org/schemas/sitemap/0.9";
        var urlset = new XElement(ns + "urlset",
            urls.Select(u =>
                new XElement(ns + "url",
                    new XElement(ns + "loc", u.Loc),
                    u.LastMod is { } lastMod
                        ? new XElement(ns + "lastmod", lastMod.ToString("yyyy-MM-dd"))
                        : null)));

        var xml = new XDocument(new XDeclaration("1.0", "utf-8", null), urlset).ToString(SaveOptions.DisableFormatting);

        cache.Set(cacheKey, xml, TimeSpan.FromHours(1));

        await Send.StringAsync(xml, cancellation: ct);
    }
}