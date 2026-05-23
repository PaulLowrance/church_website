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
    public bool IsMarkdown { get; set; }
    public bool IsPublished { get; set; }
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
        // Validate title
        if (string.IsNullOrWhiteSpace(req.Title))
        {
            ThrowError("Title is required");
            return;
        }

        // Validate body
        if (string.IsNullOrWhiteSpace(req.Body))
        {
            ThrowError("Body content is required");
            return;
        }

        // Generate or normalize slug
        var slug = string.IsNullOrWhiteSpace(req.Slug)
            ? GenerateSlug(req.Title)
            : GenerateSlug(req.Slug);

        if (string.IsNullOrWhiteSpace(slug))
        {
            ThrowError("Slug could not be generated from the provided title");
            return;
        }

        // Check uniqueness
        if (await pageRepo.SlugExistsAsync(slug))
        {
            ThrowError($"A page with slug '{slug}' already exists");
            return;
        }

        var page = new Page
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Title = req.Title.Trim(),
            Body = req.Body,
            IsMarkdown = req.IsMarkdown,
            IsPublished = req.IsPublished,
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
