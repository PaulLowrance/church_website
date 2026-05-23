using ChurchWebsite.Core.Entities;

namespace ChurchWebsite.Core.Interfaces;

public interface IPageRepository
{
    Task<Page?> GetBySlugAsync(string slug);
    Task<IEnumerable<Page>> GetAllAsync();
    Task<IEnumerable<Page>> GetNavPagesAsync();
    Task UpdateAsync(Page page);
    Task CreateAsync(Page page);
    Task<bool> SlugExistsAsync(string slug);
    Task DeleteAsync(string slug);
}
