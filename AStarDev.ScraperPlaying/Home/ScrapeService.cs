using System.Diagnostics;
using System.IO.Abstractions;
using AStarDev.FunctionalParadigm;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.ControlDb;
using AStarDev.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.ScraperPlaying.Home;

public class ScrapeService(OperationCoordinator operationCoordinator, IServiceScopeFactory scopeFactory, IFileSystem fileSystem) : IScrapeService
{
    /// <inheritdoc/>
    public async Task RunScraperAsync(IProgress<string> progress)
    {
        if (!operationCoordinator.TryStart(out var cancellationToken)) return;

        var startTime = Stopwatch.GetTimestamp();
        try
        {
            using var scope = scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var pagesProcessor = scope.ServiceProvider.GetRequiredService<IPagesProcessor>();

            progress.Report("Starting scrape operation.");
            var configuration = await LoadConfigurationAsync(unitOfWork);

            await RunSearchesAsync(pagesProcessor, configuration, progress, cancellationToken);

            progress.Report($"Search completed in: {Stopwatch.GetElapsedTime(startTime).ToDurationString()}.");
        }
        catch (HttpRequestException e)
        {
            progress.Report($"Request error: {e.Message}");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            progress.Report("Search cancelled.");
        }
        finally
        {
            operationCoordinator.Complete();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RootDirectoryExistsAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var configuration = await LoadConfigurationAsync(unitOfWork);

        return fileSystem.Directory.Exists(configuration.ScrapeDirectories.RootDirectory);
    }

    private static async Task<ScrapeConfigurationEntity> LoadConfigurationAsync(IUnitOfWork unitOfWork)
        => (await unitOfWork.GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>().TryGetFirstAsync())
            .Match(
                option => option.Match(scrapeConfig => scrapeConfig, () => throw new InvalidOperationException("Scrape configuration not found")),
                exception => throw exception
            );

    private static async Task RunSearchesAsync(IPagesProcessor pagesProcessor, ScrapeConfigurationEntity configuration, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var baseUrl = configuration.BaseUrl;
        var apiKey = configuration.UserConfiguration.ApiKey;
        var topWallpapersUrl = configuration.TopWallpapers;
        var searchCategoriesUrl = configuration.SearchStringPrefix;
        var searchCategories = configuration.SearchConfiguration.SearchCategories;

        progress.Report("Fetching top wallpapers.");
        await pagesProcessor.FetchAndProcessPagesAsync("top wallpapers", Option.None<string>(), page => topWallpapersUrl + page, apiKey, baseUrl, progress, cancellationToken);

        foreach (var category in searchCategories.Take(3))
        {
            await pagesProcessor.FetchAndProcessPagesAsync($"search category {category.Id}", Option.Some(category.Name), page => BuildCategoryUrl(page, searchCategoriesUrl, category), apiKey, baseUrl, progress, cancellationToken);
        }
    }

    private static string BuildCategoryUrl(int page, string searchCategoriesUrl, SearchCategoryEntity category)
    => page == 1
        ? searchCategoriesUrl.Replace("%7Bid%7D", category.Id)
        : searchCategoriesUrl.Replace("%7Bid%7D", category.Id) + page;
}