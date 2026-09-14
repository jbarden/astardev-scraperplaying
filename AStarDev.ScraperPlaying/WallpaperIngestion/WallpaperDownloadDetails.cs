namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <summary>The details of a wallpaper image that has just been written to disk, published via <see cref="IImageDownloadNotifier"/> so interested parties (such as a live preview) can react.</summary>
/// <param name="FilePath">The full path the image was saved to.</param>
/// <param name="Name">The wallpaper's name, matching the saved file's base name.</param>
/// <param name="CategoryLabel">The search category the wallpaper came from, or "Top Wallpapers" when it did not come from a specific search category.</param>
/// <param name="FileSizeBytes">The wallpaper's file size, in bytes.</param>
/// <param name="Width">The wallpaper's width, in pixels.</param>
/// <param name="Height">The wallpaper's height, in pixels.</param>
public sealed record WallpaperDownloadDetails(string FilePath, string Name, string CategoryLabel, int FileSizeBytes, int Width, int Height);
