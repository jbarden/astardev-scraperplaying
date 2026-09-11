using System.Diagnostics;
using AStarDev.FunctionalParadigm;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.ControlDb;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.ScraperPlaying.Home;

public class ScrapeService(OperationCoordinator operationCoordinator, IServiceScopeFactory scopeFactory) : IScrapeService
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
            ScrapeConfigurationEntity configuration = (await unitOfWork.GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>().TryGetFirstAsync())
                .Match(scrapeConfigurationEntity => scrapeConfigurationEntity
                    .Match(scrapeConfig => scrapeConfig, () => throw new InvalidOperationException("Scrape configuration not found")),
                    _ =>
                    {
                        progress.Report("Scrape configuration not found");

                        return null!;
                    }
            )!;

            var baseUrl = configuration.SearchConfiguration.BaseUrl;
            var apiKey = configuration.UserConfiguration.ApiKey;
            var sessionCookie = configuration.UserConfiguration.SessionCookie;
            var topWallpapersUrl = configuration.SearchConfiguration.TopWallpapers;
            var searchCategoriesUrl = configuration.SearchConfiguration.SearchStringPrefix;
            var searchCategories = configuration.SearchConfiguration.SearchCategories;

            progress.Report("Fetching top wallpapers.");
            await pagesProcessor.FetchAndProcessPagesAsync("top wallpapers", page => topWallpapersUrl + page, apiKey, sessionCookie, baseUrl, progress, cancellationToken);

            foreach (var category in searchCategories.Take(3))
            {
                await pagesProcessor.FetchAndProcessPagesAsync($"search category {category.Id}", page => BuildCategoryUrl(page, searchCategoriesUrl, category), apiKey, sessionCookie, baseUrl, progress, cancellationToken);
            }

            progress.Report($"Search completed in: {FormatElapsedTime(Stopwatch.GetElapsedTime(startTime))}.");
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

    private static string BuildCategoryUrl(int page, string searchCategoriesUrl, SearchCategoryEntity category)
    => page == 1
        ? searchCategoriesUrl.Replace("%7Bid%7D", category.Id)
        : searchCategoriesUrl.Replace("%7Bid%7D", category.Id) + page;

    internal static string FormatElapsedTime(TimeSpan elapsed)
    => elapsed.TotalSeconds < 60
        ? $"{elapsed.TotalSeconds:0.##} seconds"
        : elapsed.TotalMinutes < 60
            ? $"{elapsed.TotalMinutes:0.##} minutes"
            : $"{elapsed.TotalHours:0.##} hours";
}