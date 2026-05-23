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

    public async Task UpdateAsync(Page page)
    {
        using var conn = factory.CreateConnection();
        await conn.ExecuteAsync(
            @"UPDATE pages SET title = @Title, body = @Body, is_markdown = @IsMarkdown, updated_at = @UpdatedAt WHERE slug = @Slug",
            page);
    }

    public async Task CreateAsync(Page page)
    {
        using var conn = factory.CreateConnection();
        await conn.ExecuteAsync(
            @"INSERT INTO pages (id, slug, title, body, is_markdown, updated_at) VALUES (@Id, @Slug, @Title, @Body, @IsMarkdown, @UpdatedAt)",
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
}
