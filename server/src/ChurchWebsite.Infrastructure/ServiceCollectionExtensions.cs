using ChurchWebsite.Core.Interfaces;
using ChurchWebsite.Infrastructure.Data;
using ChurchWebsite.Infrastructure.Repositories;
using ChurchWebsite.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ChurchWebsite.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton(new DbConnectionFactory(connectionString));
        services.AddSingleton<DbInitializer>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPageRepository, PageRepository>();
        services.AddScoped<IPodcastEpisodeRepository, PodcastEpisodeRepository>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        return services;
    }
}
