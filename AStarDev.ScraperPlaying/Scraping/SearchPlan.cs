using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Works out which searches a scrape runs from the scrape configuration.</summary>
public static class SearchPlan
{
    /// <summary>Builds a search for each configured category, followed by the "Top Wallpapers" search.</summary>
    /// <param name="configuration">The scrape configuration.</param>
    /// <returns>The searches in the order they should run.</returns>
    public static IReadOnlyList<SearchRequest> Build(ScrapeConfigurationEntity configuration)
    {
        var topWallpapersUrl = configuration.TopWallpapers;
        var searchCategoriesUrl = configuration.SearchStringPrefix;
        var searchCategoriesSuffix = configuration.SearchStringSuffix;

        return
        [
            .. configuration.SearchConfiguration.SearchCategories.Select(category => new SearchRequest(
                $"search category {category.Id}",
                Option.Some(category.Name),
                page => WallhavenUrlBuilder.BuildCategoryPageUrl(searchCategoriesUrl, category, searchCategoriesSuffix, page))),
            new SearchRequest("top wallpapers", Option.None<string>(), page => WallhavenUrlBuilder.BuildTopWallpapersPageUrl(topWallpapersUrl, page))
        ];
    }
}
