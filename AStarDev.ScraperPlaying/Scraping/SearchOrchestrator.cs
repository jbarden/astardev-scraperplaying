using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.Scraping;

/// <inheritdoc/>
public sealed class SearchOrchestrator(IPagesProcessor pagesProcessor) : ISearchOrchestrator
{
    private const int MaximumSearchCategories = 3;

    /// <inheritdoc/>
    public async Task RunSearchesAsync(ScrapeConfigurationEntity configuration, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var connection = new WallhavenConnection(configuration.UserConfiguration.ApiKey, configuration.BaseUrl);
        var topWallpapersUrl = configuration.TopWallpapers;
        var searchCategoriesUrl = configuration.SearchStringPrefix;
        var searchCategoriesSuffix = configuration.SearchStringSuffix;
        IReadOnlyList<string> personCategories = [.. configuration.SearchConfiguration.PersonCategories.Select(category => category.Name)];

        progress.Report("Fetching categories.");
        foreach (var category in configuration.SearchConfiguration.SearchCategories.Take(MaximumSearchCategories))
        {
            await pagesProcessor.FetchAndProcessPagesAsync($"search category {category.Name}", Option.Some(category.Name), page => WallhavenUrlBuilder.BuildCategoryPageUrl(searchCategoriesUrl, searchCategoriesSuffix, category, page), connection, personCategories, progress, cancellationToken);
        }

        progress.Report("Fetching top wallpapers.");
        await pagesProcessor.FetchAndProcessPagesAsync("top wallpapers", Option.None<string>(), page => WallhavenUrlBuilder.BuildTopWallpapersPageUrl(topWallpapersUrl, page), connection, personCategories, progress, cancellationToken);
    }
}
