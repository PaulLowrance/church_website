using Dapper;
using ChurchWebsite.Core.Entities;
using ChurchWebsite.Core.Interfaces;
using ChurchWebsite.Infrastructure.Data;

namespace ChurchWebsite.Infrastructure.Repositories;

public class PageRepository(DbConnectionFactory factory) : IPageRepository
{
    public async Task<Page?> GetBySlugAsync(string slug)
    {
        using var conn = factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Page>(
            "SELECT * FROM pages WHERE slug = @slug",
            new { slug });
    }

    public async Task<IEnumerable<Page>> GetAllAsync()
    {
        using var conn = factory.CreateConnection();
        return await conn.QueryAsync<Page>("SELECT * FROM pages ORDER BY title");
    }

    public async Task<IEnumerable<Page>> GetNavPagesAsync()
    {
        using var conn = factory.CreateConnection();
        return await conn.QueryAsync<Page>(
            "SELECT * FROM pages WHERE show_in_nav = TRUE AND is_published = TRUE ORDER BY title");
    }

    public async Task UpdateAsync(Page page)
    {
        using var conn = factory.CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE pages SET title = @Title, body = @Body, is_markdown = @IsMarkdown, is_published = @IsPublished, show_in_nav = @ShowInNav, nav_title = @NavTitle, updated_at = @UpdatedAt WHERE slug = @Slug",
            page);
    }

    public async Task CreateAsync(Page page)
    {
        using var conn = factory.CreateConnection();
        await conn.ExecuteAsync(
            @"INSERT INTO pages (id, slug, title, body, is_markdown, is_published, show_in_nav, nav_title, updated_at) VALUES (@Id, @Slug, @Title, @Body, @IsMarkdown, @IsPublished, @ShowInNav, @NavTitle, @UpdatedAt)",
            page);
    }

    public async Task<bool> SlugExistsAsync(string slug)
    {
        using var conn = factory.CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM pages WHERE slug = @slug",
            new { slug });
        return count > 0;
    }

    public async Task DeleteAsync(string slug)
    {
        using var conn = factory.CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM pages WHERE slug = @slug",
            new { slug });
    }
}
