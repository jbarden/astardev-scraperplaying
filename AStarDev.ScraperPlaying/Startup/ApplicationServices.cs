using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            .AddSingleton<IScrapeConfigurationFileWriter, ScrapeConfigurationFileWriter>()
            .AddSingleton<IScrapeConfigurationExportService, ScrapeConfigurationExportService>()
            .AddSingleton<IScrapeConfigurationExporter, ScrapeConfigurationExporter>()
            .AddSingleton<OperationCoordinator>()
            .AddScoped<IPagesProcessor, PagesProcessor>()
            .AddScoped<IJsonResponseProcessor, JsonResponseProcessor>()
            .AddScoped<IImageProcessor, ImageProcessor>()
            .AddSingleton<Func<DateTimeOffset>>(_ => () => DateTimeOffset.UtcNow)
            .AddSingleton<MainWindow>()
            .AddHttpClient(PagesProcessor.HttpClientName, client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }).Services;
}
