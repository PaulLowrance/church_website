using ChurchWebsite.Core.Entities;

namespace ChurchWebsite.Core.Interfaces;

public class AuthResult
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public interface IAuthService
{
    Task<AuthResult?> AuthenticateAsync(string username, string password);
    Task<User?> GetUserByUsernameAsync(string username);
}
