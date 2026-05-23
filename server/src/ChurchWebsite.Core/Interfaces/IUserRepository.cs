using ChurchWebsite.Core.Entities;

namespace ChurchWebsite.Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task CreateAsync(User user);
}
