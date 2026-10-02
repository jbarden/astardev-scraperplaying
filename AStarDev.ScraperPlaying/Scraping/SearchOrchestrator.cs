using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.Scraping;

/// <inheritdoc/>
public sealed class SearchOrchestrator(IPagesProcessor pagesProcessor, ScrapeLimits limits) : ISearchOrchestrator
{
    /// <summary>The hot wallpapers are saved and labelled like a category of this name, so they land in the "hot-wallpapers" directory.</summary>
    private const string HotWallpapersName = "Hot Wallpapers";

    /// <inheritdoc/>
    public async Task RunSearchesAsync(ScrapeConfigurationEntity configuration, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var target = new ScrapeTarget(new WallhavenConnection(configuration.UserConfiguration.ApiKey, configuration.BaseUrl), [.. configuration.SearchConfiguration.PersonCategories.Select(category => category.Name)]);

        progress.Report("Fetching hot wallpapers.");
        _ = await pagesProcessor.FetchAndProcessPagesAsync(HotRequest(configuration, target), progress, cancellationToken);

        progress.Report("Fetching top wallpapers.");
        _ = await pagesProcessor.FetchAndProcessPagesAsync(TopRequest(configuration, target), progress, cancellationToken);

        progress.Report("Fetching categories.");
        foreach (var category in configuration.SearchConfiguration.SearchCategories.Where(category => category.IncludeInSearch).Take(limits.MaximumSearchCategories))
        {
            _ = await pagesProcessor.FetchAndProcessPagesAsync(CategoryRequest(configuration, category, target), progress, cancellationToken);
        }
    }

    private static PageScrapeRequest HotRequest(ScrapeConfigurationEntity configuration, ScrapeTarget target)
        => new(new ScrapeLabel("hot wallpapers", Option.Some(HotWallpapersName)), Option.None<SearchCategoryProgress>(), new PageHooks(_ => { }, page => WallhavenUrlBuilder.BuildHotWallpapersPageUrl(configuration.HotWallpapers, page)), target);

    private static PageScrapeRequest TopRequest(ScrapeConfigurationEntity configuration, ScrapeTarget target)
        => new(new ScrapeLabel("top wallpapers", Option.None<string>()), Option.None<SearchCategoryProgress>(), new PageHooks(_ => { }, page => WallhavenUrlBuilder.BuildTopWallpapersPageUrl(configuration.TopWallpapers, page)), target);

    private static PageScrapeRequest CategoryRequest(ScrapeConfigurationEntity configuration, SearchCategoryEntity category, ScrapeTarget target)
        => new(new ScrapeLabel($"search category {category.Name}", Option.Some(category.Name)), Option.Some(new SearchCategoryProgress(category.LastKnownImageCount, category.LastPageVisited, category.TotalPages)), new PageHooks(RecordProgress(category), page => WallhavenUrlBuilder.BuildCategoryPageUrl(configuration.SearchStringPrefix, configuration.SearchStringSuffix, category, page)), target);

    private static Action<SearchCategoryProgress> RecordProgress(SearchCategoryEntity category)
        => scraped => category.RecordScrapeProgress(scraped.LastKnownImageCount, scraped.LastPageVisited, scraped.TotalPages);
}
