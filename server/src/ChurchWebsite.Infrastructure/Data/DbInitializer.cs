using System.Data;
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
                content_type VARCHAR(20) NOT NULL DEFAULT 'wysiwyg',
                is_published BOOLEAN NOT NULL DEFAULT TRUE,
                show_in_nav BOOLEAN NOT NULL DEFAULT TRUE,
                nav_title VARCHAR(25) NOT NULL DEFAULT '',
                updated_at TIMESTAMP WITH TIME ZONE NOT NULL
            );";
        await conn.ExecuteAsync(createPagesSql);

        await MigrateColumnAsync(conn, "is_published", "BOOLEAN NOT NULL DEFAULT TRUE");
        await MigrateColumnAsync(conn, "show_in_nav", "BOOLEAN NOT NULL DEFAULT TRUE");
        await MigrateColumnAsync(conn, "nav_title", "VARCHAR(25) NOT NULL DEFAULT ''");

        var contentTypeAdded = await MigrateColumnAsync(conn, "content_type", "VARCHAR(20) NOT NULL DEFAULT 'wysiwyg'");
        if (contentTypeAdded)
        {
            await conn.ExecuteAsync(@"
                UPDATE pages
                SET content_type = CASE WHEN is_markdown = TRUE THEN 'markdown' ELSE 'wysiwyg' END
                WHERE is_markdown IS NOT NULL");
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
                ContentType = "wysiwyg",
                IsPublished = true,
                ShowInNav = true,
                NavTitle = "Welcome",
                UpdatedAt = DateTime.UtcNow
            };
            await conn.ExecuteAsync(
                "INSERT INTO pages (id, slug, title, body, content_type, is_published, show_in_nav, nav_title, updated_at) VALUES (@Id, @Slug, @Title, @Body, @ContentType, @IsPublished, @ShowInNav, @NavTitle, @UpdatedAt)",
                page);
        }
        else
        {
            // Backfill existing rows that lack nav values
            await conn.ExecuteAsync(
                "UPDATE pages SET show_in_nav = TRUE, nav_title = LEFT(title, 25) WHERE nav_title = '' OR nav_title IS NULL;");
        }
    }

    private static async Task<bool> MigrateColumnAsync(IDbConnection conn, string columnName, string columnDef)
    {
        var exists = await conn.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*) FROM information_schema.columns
              WHERE table_name = 'pages' AND column_name = @columnName",
            new { columnName });
        if (exists == 0)
        {
            await conn.ExecuteAsync(
                $"ALTER TABLE pages ADD COLUMN {columnName} {columnDef};");
            return true;
        }
        return false;
    }
}
