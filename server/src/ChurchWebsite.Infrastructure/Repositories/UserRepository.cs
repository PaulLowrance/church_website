using Dapper;
using ChurchWebsite.Core.Entities;
using ChurchWebsite.Core.Interfaces;
using ChurchWebsite.Infrastructure.Data;

namespace ChurchWebsite.Infrastructure.Repositories;

public class UserRepository(DbConnectionFactory factory) : IUserRepository
{
    public async Task<User?> GetByUsernameAsync(string username)
    {
        using var conn = factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM users WHERE username = @username",
            new { username });
    }

    public async Task CreateAsync(User user)
    {
        using var conn = factory.CreateConnection();
        await conn.ExecuteAsync(
            "INSERT INTO users (id, username, password_hash, role, created_at) VALUES (@Id, @Username, @PasswordHash, @Role, @CreatedAt)",
            user);
    }
}
