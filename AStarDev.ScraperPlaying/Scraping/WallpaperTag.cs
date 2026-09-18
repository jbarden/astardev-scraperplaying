namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>A tag belonging to a wallpaper, from whichever source it was fetched or scraped.</summary>
/// <param name="WallhavenTagId">Wallhaven's own numeric id for this tag.</param>
/// <param name="Name">The tag's display name.</param>
/// <param name="Alias">The tag's URL-safe alias, or empty when not known.</param>
/// <param name="CategoryId">Wallhaven's category id for this tag, or 0 when not known.</param>
/// <param name="Category">The tag's category name, or empty when not known.</param>
/// <param name="Purity">The tag's purity rating, or empty when not known.</param>
public sealed record WallpaperTag(int WallhavenTagId, string Name, string Alias = "", int CategoryId = 0, string Category = "", string Purity = "");
