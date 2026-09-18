namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>The raw shape returned by the wallpaper detail page's DOM-scraping script.</summary>
/// <param name="ImageUrl">The full-size image URL, or an empty string when the image element was not found.</param>
/// <param name="DimensionX">The image width in pixels, or 0 when the resolution text could not be parsed.</param>
/// <param name="DimensionY">The image height in pixels, or 0 when the resolution text could not be parsed.</param>
/// <param name="Tags">The tags listed on the page.</param>
public sealed record WallpaperDetailScrapeResult(string ImageUrl, int DimensionX, int DimensionY, IReadOnlyList<WallpaperTag> Tags);
