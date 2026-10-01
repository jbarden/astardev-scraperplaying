using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.Scraping;

/// <inheritdoc/>
public sealed class SearchOrchestrator(IPagesProcessor pagesProcessor, IUnitOfWork unitOfWork, ScrapeLimits limits) : ISearchOrchestrator
{
    /// <inheritdoc/>
    public async Task RunSearchesAsync(ScrapeConfigurationEntity configuration, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var connection = new WallhavenConnection(configuration.UserConfiguration.ApiKey, configuration.BaseUrl);
        var topWallpapersUrl = configuration.TopWallpapers;
        var searchCategoriesUrl = configuration.SearchStringPrefix;
        var searchCategoriesSuffix = configuration.SearchStringSuffix;
        IReadOnlyList<string> personCategories = [.. configuration.SearchConfiguration.PersonCategories.Select(category => category.Name)];

        progress.Report("Fetching categories.");
        foreach (var category in configuration.SearchConfiguration.SearchCategories.Take(limits.MaximumSearchCategories))
        {
            var scrapedProgress = await pagesProcessor.FetchAndProcessPagesAsync($"search category {category.Name}", Option.Some(category.Name), Option.Some(new SearchCategoryProgress(category.LastKnownImageCount, category.LastPageVisited, category.TotalPages)), pageProgress => category.RecordScrapeProgress(pageProgress.LastKnownImageCount, pageProgress.LastPageVisited, pageProgress.TotalPages), page => WallhavenUrlBuilder.BuildCategoryPageUrl(searchCategoriesUrl, searchCategoriesSuffix, category, page), connection, personCategories, progress, cancellationToken);
            if (scrapedProgress.TryGetValue(out var scraped))
            {
                category.RecordScrapeProgress(scraped.LastKnownImageCount, scraped.LastPageVisited, scraped.TotalPages);
                _ = await unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        progress.Report("Fetching top wallpapers.");
        _ = await pagesProcessor.FetchAndProcessPagesAsync("top wallpapers", Option.None<string>(), Option.None<SearchCategoryProgress>(), _ => { }, page => WallhavenUrlBuilder.BuildTopWallpapersPageUrl(topWallpapersUrl, page), connection, personCategories, progress, cancellationToken);
    }
}
