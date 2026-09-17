using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Builds the Wallhaven page URLs used to fetch "top wallpapers" and search-category results.</summary>
public static class WallhavenUrlBuilder
{
    /// <summary>Builds the URL for the given page of the "top wallpapers" search.</summary>
    /// <param name="topWallpapersUrl">The configured base URL for the "top wallpapers" search.</param>
    /// <param name="page">The page number to append.</param>
    public static string BuildTopWallpapersPageUrl(string topWallpapersUrl, int page)
        => topWallpapersUrl + page;

    /// <summary>
    /// Builds the URL for the given page of a search category by appending <paramref name="category"/>'s id
    /// to <paramref name="searchCategoriesUrl"/>, followed by the page number.
    /// </summary>
    /// <param name="searchCategoriesUrl">The configured search URL prefix, e.g. "https://wallhaven.cc/search?q=id:".</param>
    /// <param name="category">The search category whose id is substituted into the template.</param>
    /// <param name="page">The page number to append, omitted for page 1.</param>
    public static string BuildCategoryPageUrl(string searchCategoriesUrl, SearchCategoryEntity category, string searchCategoriesSuffix, int page) => $"{searchCategoriesUrl}{category.Id}{searchCategoriesSuffix}{page}";
}
