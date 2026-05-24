using System.Text.RegularExpressions;
using ChurchWebsite.Core.Entities;
using ChurchWebsite.Core.Interfaces;
using FastEndpoints;

namespace ChurchWebsite.Api.Endpoints.Pages;

public class CreatePageRequest
{
    public string? Slug { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string ContentType { get; set; } = "wysiwyg";
    public bool IsPublished { get; set; }
    public bool ShowInNav { get; set; } = true;
    public string? NavTitle { get; set; }
}

public class CreatePageResponse
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}

public class CreatePageEndpoint(IPageRepository pageRepo) : Endpoint<CreatePageRequest, CreatePageResponse>
{
    public override void Configure()
    {
        Post("/api/pages");
        Roles("Admin");
    }

    public override async Task HandleAsync(CreatePageRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Title))
        {
            ThrowError("Title is required");
            return;
        }

        if (string.IsNullOrWhiteSpace(req.Body))
        {
            ThrowError("Body content is required");
            return;
        }

        var slug = string.IsNullOrWhiteSpace(req.Slug)
            ? GenerateSlug(req.Title)
            : GenerateSlug(req.Slug);

        if (string.IsNullOrWhiteSpace(slug))
        {
            ThrowError("Slug could not be generated from the provided title");
            return;
        }

        if (await pageRepo.SlugExistsAsync(slug))
        {
            ThrowError($"A page with slug '{slug}' already exists");
            return;
        }

        var navTitle = string.IsNullOrWhiteSpace(req.NavTitle) ? req.Title.Trim() : req.NavTitle.Trim();
        if (navTitle.Length > 25)
            navTitle = navTitle[..25];

        var page = new Page
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Title = req.Title.Trim(),
            Body = req.Body,
            ContentType = req.ContentType,
            IsPublished = req.IsPublished,
            ShowInNav = req.ShowInNav,
            NavTitle = navTitle,
            UpdatedAt = DateTime.UtcNow
        };

        await pageRepo.CreateAsync(page);

        await Send.OkAsync(new CreatePageResponse
        {
            Slug = page.Slug,
            Title = page.Title
        }, cancellation: ct);
    }

    private static string GenerateSlug(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var slug = input.Trim().ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        slug = slug.Trim('-');

        return slug;
    }
}
