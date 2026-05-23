using Dapper;
using ChurchWebsite.Core.Entities;

namespace ChurchWebsite.Infrastructure.Data;

public class DbInitializer(DbConnectionFactory factory)
{
    public async Task InitializeAsync()
    {
        using var conn = factory.CreateConnection();

        var createUsersSql = @"
            CREATE TABLE IF NOT EXISTS users (
                id UUID PRIMARY KEY,
                username VARCHAR(50) UNIQUE NOT NULL,
                password_hash TEXT NOT NULL,
                role VARCHAR(20) NOT NULL,
                created_at TIMESTAMP WITH TIME ZONE NOT NULL
            );";
        await conn.ExecuteAsync(createUsersSql);

        var createPagesSql = @"
            CREATE TABLE IF NOT EXISTS pages (
                id UUID PRIMARY KEY,
                slug VARCHAR(50) UNIQUE NOT NULL,
                title VARCHAR(200) NOT NULL,
                body TEXT NOT NULL,
                is_markdown BOOLEAN NOT NULL DEFAULT FALSE,
                is_published BOOLEAN NOT NULL DEFAULT TRUE,
                updated_at TIMESTAMP WITH TIME ZONE NOT NULL
            );";
        await conn.ExecuteAsync(createPagesSql);

        // Migrate existing tables that lack is_published
        var columnExists = await conn.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*) FROM information_schema.columns
              WHERE table_name = 'pages' AND column_name = 'is_published'");
        if (columnExists == 0)
        {
            await conn.ExecuteAsync(
                "ALTER TABLE pages ADD COLUMN is_published BOOLEAN NOT NULL DEFAULT TRUE;");
        }

        var adminExists = await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM users WHERE username = @username", new { username = "admin" });

        if (adminExists is null)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("testing123"),
                Role = "Admin",
                CreatedAt = DateTime.UtcNow
            };
            await conn.ExecuteAsync(
                "INSERT INTO users (id, username, password_hash, role, created_at) VALUES (@Id, @Username, @PasswordHash, @Role, @CreatedAt)",
                user);
        }

        var homeExists = await conn.QueryFirstOrDefaultAsync<Page>(
            "SELECT * FROM pages WHERE slug = @slug", new { slug = "home" });

        if (homeExists is null)
        {
            var page = new Page
            {
                Id = Guid.NewGuid(),
                Slug = "home",
                Title = "Welcome to Our Church",
                Body = "<h1>Welcome</h1><p>This is the home page for our church website.</p>",
                IsMarkdown = false,
                IsPublished = true,
                UpdatedAt = DateTime.UtcNow
            };
            await conn.ExecuteAsync(
                "INSERT INTO pages (id, slug, title, body, is_markdown, is_published, updated_at) VALUES (@Id, @Slug, @Title, @Body, @IsMarkdown, @IsPublished, @UpdatedAt)",
                page);
        }
    }
}
