using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.Scraping;

/// <inheritdoc/>
public sealed class SearchOrchestrator(IPagesProcessor pagesProcessor, IUnitOfWork unitOfWork, ScrapeLimits limits) : ISearchOrchestrator
{
    /// <summary>The hot wallpapers are saved and labelled like a category of this name, so they land in the "hot-wallpapers" directory.</summary>
    private const string HotWallpapersName = "Hot Wallpapers";

    /// <inheritdoc/>
    public async Task RunSearchesAsync(ScrapeConfigurationEntity configuration, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var connection = new WallhavenConnection(configuration.UserConfiguration.ApiKey, configuration.BaseUrl);
        IReadOnlyList<string> personCategories = [.. configuration.SearchConfiguration.PersonCategories.Select(category => category.Name)];

        progress.Report("Fetching hot wallpapers.");
        _ = await pagesProcessor.FetchAndProcessPagesAsync(new PageScrapeRequest("hot wallpapers", Option.Some(HotWallpapersName), Option.None<SearchCategoryProgress>(), _ => { }, page => WallhavenUrlBuilder.BuildHotWallpapersPageUrl(configuration.HotWallpapers, page), connection, personCategories), progress, cancellationToken);

        progress.Report("Fetching top wallpapers.");
        _ = await pagesProcessor.FetchAndProcessPagesAsync(new PageScrapeRequest("top wallpapers", Option.None<string>(), Option.None<SearchCategoryProgress>(), _ => { }, page => WallhavenUrlBuilder.BuildTopWallpapersPageUrl(configuration.TopWallpapers, page), connection, personCategories), progress, cancellationToken);

        progress.Report("Fetching categories.");
        foreach (var category in configuration.SearchConfiguration.SearchCategories.Where(category => category.IncludeInSearch).Take(limits.MaximumSearchCategories))
        {
            await ScrapeCategoryAsync(category, new PageScrapeRequest($"search category {category.Name}", Option.Some(category.Name), Option.Some(new SearchCategoryProgress(category.LastKnownImageCount, category.LastPageVisited, category.TotalPages)), RecordProgress(category), page => WallhavenUrlBuilder.BuildCategoryPageUrl(configuration.SearchStringPrefix, configuration.SearchStringSuffix, category, page), connection, personCategories), progress, cancellationToken);
        }
    }

    private static Action<SearchCategoryProgress> RecordProgress(SearchCategoryEntity category)
        => scraped => category.RecordScrapeProgress(scraped.LastKnownImageCount, scraped.LastPageVisited, scraped.TotalPages);

    private async Task ScrapeCategoryAsync(SearchCategoryEntity category, PageScrapeRequest request, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var scrapedProgress = await pagesProcessor.FetchAndProcessPagesAsync(request, progress, cancellationToken);
        if (!scrapedProgress.TryGetValue(out var scraped)) return;

        RecordProgress(category)(scraped);
        _ = await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
