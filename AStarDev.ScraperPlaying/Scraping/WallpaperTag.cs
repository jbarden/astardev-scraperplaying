namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>A tag scraped from a wallpaper's detail page.</summary>
/// <param name="WallhavenTagId">Wallhaven's own numeric id for this tag.</param>
/// <param name="Name">The tag's display name.</param>
public sealed record WallpaperTag(int WallhavenTagId, string Name);
