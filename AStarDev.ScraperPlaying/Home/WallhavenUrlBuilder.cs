using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.Home;

/// <summary>
/// Builds the Wallhaven page URLs used to fetch "top wallpapers" and search-category results.
/// </summary>
public static class WallhavenUrlBuilder
{
    /// <summary>Builds the URL for the given page of the "top wallpapers" search.</summary>
    /// <param name="topWallpapersUrl">The configured base URL for the "top wallpapers" search.</param>
    /// <param name="page">The page number to append.</param>
    public static string BuildTopWallpapersPageUrl(string topWallpapersUrl, int page)
        => topWallpapersUrl + page;

    /// <summary>
    /// Builds the URL for the given page of a search category, substituting <paramref name="category"/>'s id
    /// into the "%7Bid%7D" placeholder in <paramref name="searchCategoriesUrl"/>. The page number is omitted
    /// for page 1 to match Wallhaven's URL convention.
    /// </summary>
    /// <param name="searchCategoriesUrl">The configured search URL template, containing a "%7Bid%7D" placeholder.</param>
    /// <param name="category">The search category whose id is substituted into the template.</param>
    /// <param name="page">The page number to append, omitted for page 1.</param>
    public static string BuildCategoryPageUrl(string searchCategoriesUrl, SearchCategoryEntity category, int page)
    {
        var url = searchCategoriesUrl.Replace("%7Bid%7D", category.Id);

        return page == 1 ? url : url + page;
    }
}
