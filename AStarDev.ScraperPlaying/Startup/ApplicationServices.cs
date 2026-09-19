using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.UI;
using AStarDev.ScraperPlaying.WallpaperIngestion;

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
            .AddSingleton<IScrapeConfigurationFileWriter, ScrapeConfigurationFileWriter>()
            .AddSingleton<IScrapeConfigurationExportService, ScrapeConfigurationExportService>()
            .AddSingleton<IScrapeConfigurationExporter, ScrapeConfigurationExporter>()
            .AddSingleton<IScrapeConfigurationFileService, ScrapeConfigurationFileService>()
            .AddSingleton<IImageDownloadNotifier, ImageDownloadNotifier>()
            .AddSingleton<IDownloadedImageDecoder, DownloadedImageDecoder>()
            .AddSingleton<ImageDisplayCoordinator>()
            .AddSingleton<OperationCoordinator>()
            .AddScoped<IPagesProcessor, WebsitePagesProcessor>()
            .AddScoped<IListingPageScraper, ListingPageScraper>()
            .AddScoped<IWallpaperIngestor, WallpaperIngestor>()
            .AddScoped<IWallpaperDetailPageScraper, WallpaperDetailPageScraper>()
            .AddScoped<IPlaywrightBrowserSession, PlaywrightBrowserSession>()
            .AddScoped<IWallpaperDetailImageProcessor, WallpaperDetailImageProcessor>()
            .AddScoped<ISaveDirectoryResolver, SaveDirectoryResolver>()
            .AddScoped<ITagsProcessor, TagsProcessor>()
            .AddSingleton<Func<DateTimeOffset>>(_ => () => DateTimeOffset.UtcNow)
            .AddSingleton<Func<TimeSpan>>(_ => () => TimeSpan.FromSeconds(2))
            .AddSingleton<MainWindow>();
}
