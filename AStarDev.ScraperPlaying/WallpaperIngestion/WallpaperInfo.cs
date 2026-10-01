namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <summary>The details of a wallpaper shown alongside its image.</summary>
/// <param name="Name">The saved file's actual name, including its extension and any person-name prefix.</param>
/// <param name="CategoryLabel">The search category the wallpaper came from, or "Top Wallpapers" when it did not come from a specific search category.</param>
/// <param name="FileSizeBytes">The wallpaper's file size, in bytes.</param>
/// <param name="Width">The wallpaper's width, in pixels.</param>
/// <param name="Height">The wallpaper's height, in pixels.</param>
public sealed record WallpaperInfo(string Name, string CategoryLabel, int FileSizeBytes, int Width, int Height);
