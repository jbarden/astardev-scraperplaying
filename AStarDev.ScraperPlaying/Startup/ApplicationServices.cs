using AStarDev.ScraperPlaying.Operations;
using System.Net.Http.Headers;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AStarDev.ScraperPlaying.Downloads;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.ScraperPlaying.Scoping;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.Tags;
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
            .AddSingleton<IScopedRunner, ScopedRunner>()
            .AddSingleton<IScrapeService, ScrapeService>()
            .AddSingleton<IRootDirectoryCheck, RootDirectoryCheck>()
            .AddSingleton<IConfigurationFilePicker, ConfigurationFilePicker>()
            .AddSingleton<IScrapeConfigurationFileReader, ScrapeConfigurationFileReader>()
            .AddSingleton<IScrapeConfigurationImportService, ScrapeConfigurationImportService>()
            .AddSingleton<IScrapeConfigurationImporter, ScrapeConfigurationImporter>()
            .AddSingleton<IScrapeConfigurationFileWriter, ScrapeConfigurationFileWriter>()
            .AddSingleton<IScrapeConfigurationExportService, ScrapeConfigurationExportService>()
            .AddSingleton<IScrapeConfigurationExporter, ScrapeConfigurationExporter>()
            .AddSingleton<IScrapeConfigurationFileService, ScrapeConfigurationFileService>()
            .AddSingleton<IScrapeConfigurationCatalogue, ScrapeConfigurationCatalogue>()
            .AddSingleton<IScrapeConfigurationUpdater, ScrapeConfigurationUpdater>()
            .AddSingleton<ConfigurationEditSaver>()
            .AddSingleton<IImageDownloadNotifier, ImageDownloadNotifier>()
            .AddSingleton<IDownloadedImageDecoder, DownloadedImageDecoder>()
            .AddSingleton<ImageDisplayCoordinator>()
            .AddSingleton<OperationCoordinator>()
            .AddSingleton(ScrapeLimits.From(configuration))
            .AddSingleton<IWallhavenClientFactory, WallhavenClientFactory>()
            .AddScoped<IWallhavenPageFetcher, WallhavenPageFetcher>()
            .AddScoped<IPagesProcessor, PagesProcessor>()
            .AddScoped<ISearchOrchestrator, SearchOrchestrator>()
            .AddScoped<IJsonResponseProcessor, JsonResponseProcessor>()
            .AddScoped<IImageDownloader, ImageDownloader>()
            .AddScoped<IWallpaperFileRecorder, WallpaperFileRecorder>()
            .AddScoped<INewWallpaperIngestor, NewWallpaperIngestor>()
            .AddScoped<ISaveDirectoryResolver, SaveDirectoryResolver>()
            .AddScoped<ITagsProcessor, TagsProcessor>()
            .AddScoped<IWallpaperIngestionService, WallpaperIngestionService>()
            .AddSingleton<RateLimiter>(_ => new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = ApplicationConstants.WallhavenRequestsPerWindow,
                Window = ApplicationConstants.WallhavenRateLimitWindow,
                SegmentsPerWindow = 12,
                QueueLimit = int.MaxValue,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }))
            .AddSingleton(TimeProvider.System)
            .AddSingleton(DownloadPacing.Default)
            .AddTransient<WallhavenRateLimitingHandler>()
            .AddTransient<WallhavenApiKeyHandler>()
            .AddSingleton<IDatabaseInitialization, DatabaseInitialization>()
            .AddSingleton<StatusReporter>()
            .AddSingleton<UserOperationRunner>()
            .AddSingleton<ITagCatalogue, TagCatalogue>()
            .AddSingleton<ConfigurationBrowser>()
            .AddSingleton<TagsBrowser>()
            .AddSingleton<IDownloadsClearer, DownloadsClearer>()
            .AddSingleton<ClearDownloadsRunner>()
            .AddSingleton<ScrapeRunner>()
            .AddSingleton<ApplicationReadiness>()
            .AddSingleton<MainWindow>()
            .AddHttpClient(ApplicationConstants.WallhavenHttpClientName, client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }).AddHttpMessageHandler<WallhavenApiKeyHandler>().AddHttpMessageHandler<WallhavenRateLimitingHandler>().Services;
}
