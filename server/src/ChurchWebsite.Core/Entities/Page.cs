namespace ChurchWebsite.Core.Entities;

public class Page
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsMarkdown { get; set; }
    public bool IsPublished { get; set; } = true;
    public bool ShowInNav { get; set; } = true;
    public string NavTitle { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
