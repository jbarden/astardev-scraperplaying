namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>A wallpaper's image and tags, scraped from its own detail page.</summary>
/// <param name="WallpaperId">Wallhaven's id for the wallpaper.</param>
/// <param name="ImageUrl">The full-size image URL, or an empty string when it could not be found on the page.</param>
/// <param name="DimensionX">The image width in pixels, or 0 when it could not be parsed from the page.</param>
/// <param name="DimensionY">The image height in pixels, or 0 when it could not be parsed from the page.</param>
/// <param name="Tags">The tags listed on the page.</param>
public sealed record WallpaperDetail(string WallpaperId, string ImageUrl, int DimensionX, int DimensionY, IReadOnlyList<WallpaperTag> Tags);
