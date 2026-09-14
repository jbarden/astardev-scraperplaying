using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <summary>A wallpaper together with the extension and directory its image file should be downloaded to and its file record processed against.</summary>
/// <param name="Wallpaper">The wallpaper data being downloaded/processed.</param>
/// <param name="Directory">The directory to save the wallpaper's image into.</param>
/// <param name="Extension">The file extension (including the leading '.') to save the image with.</param>
/// <param name="CategoryLabel">The search category the wallpaper came from, or "Top Wallpapers" when it did not come from a specific search category.</param>
public sealed record WallpaperFileRequest(Data Wallpaper, string Directory, string Extension, string CategoryLabel);
