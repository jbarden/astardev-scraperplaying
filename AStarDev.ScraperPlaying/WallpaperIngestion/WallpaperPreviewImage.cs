namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <summary>A decoded, display-ready wallpaper image together with the details to show alongside it.</summary>
/// <param name="PngStream">A stream, positioned at the start, containing the PNG-encoded image.</param>
/// <param name="Name">The saved file's actual name, including its extension and any person-name prefix.</param>
/// <param name="CategoryLabel">The search category the wallpaper came from, or "Top Wallpapers" when it did not come from a specific search category.</param>
/// <param name="FileSizeBytes">The wallpaper's file size, in bytes.</param>
/// <param name="Width">The wallpaper's width, in pixels.</param>
/// <param name="Height">The wallpaper's height, in pixels.</param>
public sealed record WallpaperPreviewImage(Stream PngStream, string Name, string CategoryLabel, int FileSizeBytes, int Width, int Height);
