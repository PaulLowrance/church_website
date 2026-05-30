using System.Text;
using System.Xml.Linq;
using ChurchWebsite.Core.Interfaces;
using FastEndpoints;

namespace ChurchWebsite.Api.Endpoints.Podcast;

public class PodcastRssEndpoint(IPodcastEpisodeRepository repo, IFileStorageService fileStorage)
    : EndpointWithoutRequest<string>
{
    public override void Configure()
    {
        Get("/podcast/rss");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var episodes = await repo.GetPublishedAsync();
        var baseUrl = Config["Podcast:BaseUrl"] ?? "https://bhpbc.org";
        var title = Config["Podcast:Title"] ?? "Bethlehem Haven Primitive Baptist Church Sermons";
        var description = Config["Podcast:Description"] ?? "Sermons from Bethlehem Haven Primitive Baptist Church";
        var author = Config["Podcast:Author"] ?? "Bethlehem Haven Primitive Baptist Church";
        var imageUrl = Config["Podcast:ImageUrl"] ?? "";
        var category = Config["Podcast:Category"] ?? "Religion & Spirituality";

        var rss = new XElement("rss",
            new XAttribute(XNamespace.Xmlns + "itunes", "http://www.itunes.com/dtds/podcast-1.0.dtd"),
            new XAttribute("version", "2.0"),
            new XElement("channel",
                new XElement("title", title),
                new XElement("link", baseUrl),
                new XElement("description", description),
                new XElement("language", "en-us"),
                new XElement("lastBuildDate", DateTime.UtcNow.ToString("r")),
                new XElement("generator", "ChurchWebsite"),
                new XElement("{http://www.itunes.com/dtds/podcast-1.0.dtd}author", author),
                new XElement("{http://www.itunes.com/dtds/podcast-1.0.dtd}summary", description),
                new XElement("{http://www.itunes.com/dtds/podcast-1.0.dtd}category", category),
                new XElement("{http://www.itunes.com/dtds/podcast-1.0.dtd}explicit", "clean"),
                !string.IsNullOrWhiteSpace(imageUrl)
                    ? new XElement("{http://www.itunes.com/dtds/podcast-1.0.dtd}image",
                        new XAttribute("href", imageUrl))
                    : null,
                episodes.Select(episode =>
                {
                    var audioUrl = fileStorage.GetPublicUrl(episode.AudioFilePath);
                    if (!audioUrl.StartsWith("http"))
                    {
                        audioUrl = baseUrl.TrimEnd('/') + audioUrl;
                    }

                    var item = new XElement("item",
                        new XElement("title", episode.Title),
                        new XElement("pubDate", episode.PublishedAt.ToString("r")),
                        new XElement("guid", $"{baseUrl.TrimEnd('/')}/podcast/episodes/{episode.Id}"),
                        new XElement("description", episode.Description ?? episode.Title),
                        new XElement("{http://www.itunes.com/dtds/podcast-1.0.dtd}author", author),
                        new XElement("{http://www.itunes.com/dtds/podcast-1.0.dtd}summary", episode.Description ?? episode.Title),
                        new XElement("{http://www.itunes.com/dtds/podcast-1.0.dtd}duration", ""),
                        new XElement("enclosure",
                            new XAttribute("url", audioUrl),
                            new XAttribute("length", episode.AudioFileSize),
                            new XAttribute("type", episode.AudioContentType)),
                        new XElement("category", category)
                    );

                    if (!string.IsNullOrWhiteSpace(episode.SpeakerName))
                    {
                        item.Add(new XElement("{http://www.itunes.com/dtds/podcast-1.0.dtd}author", episode.SpeakerName));
                    }

                    foreach (var tag in episode.Tags)
                    {
                        item.Add(new XElement("category", tag));
                    }

                    return item;
                })
            )
        );

        var declaration = new XDeclaration("1.0", "utf-8", null);
        var doc = new XDocument(declaration, rss);

        var xmlBuilder = new StringBuilder();
        using (var writer = new Utf8StringWriter(xmlBuilder))
        {
            doc.Save(writer);
        }

        HttpContext.Response.ContentType = "application/rss+xml; charset=utf-8";
        await HttpContext.Response.WriteAsync(xmlBuilder.ToString(), ct);
    }

    private class Utf8StringWriter : StringWriter
    {
        private readonly StringBuilder _builder;

        public Utf8StringWriter(StringBuilder builder) : base(builder)
        {
            _builder = builder;
        }

        public override Encoding Encoding => Encoding.UTF8;
    }
}
