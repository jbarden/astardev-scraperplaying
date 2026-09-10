using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using AStarDev.ControlDb;
using AStarDev.Utilities;
using AStarDev.ScraperPlaying.Home;
using AStarDev.ScraperPlaying.SearchAPI;

namespace AStarDev.ScraperPlaying.Startup;

/// <summary>Registers the application's services with the dependency injection container.</summary>
public static class ApplicationServices
{
    /// <summary>Registers configuration, infrastructure, scraping, and UI services with the dependency injection container.</summary>
    /// <param name="services">The service collection to register the application's services with.</param>
    /// <param name="configuration">The application configuration used to bind the options sections.</param>
    /// <returns>The <paramref name="services" /> collection to allow further chaining.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddSingleton<IApplicationDirectories, ApplicationDirectories>()
            .AddSingleton<IScrapeService, ScrapeService>()
            .AddSingleton<IConfigurationFilePicker, ConfigurationFilePicker>()
            .AddSingleton<IScrapeConfigurationFileReader, ScrapeConfigurationFileReader>()
            .AddSingleton<IScrapeConfigurationImportService, ScrapeConfigurationImportService>()
            .AddSingleton<IScrapeConfigurationImporter, ScrapeConfigurationImporter>()
            .AddSingleton<OperationCoordinator>()
            .AddSingleton<Func<DateTimeOffset>>(_ => () => DateTimeOffset.UtcNow)
            .AddSingleton<MainWindow>();
}
