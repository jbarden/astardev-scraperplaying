using AStarDev.ControlDb.FileDetail;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <summary>A wallpaper image that has been downloaded and saved to disk.</summary>
/// <param name="FileName">The name the image was saved under.</param>
/// <param name="SavedPath">The full path the image was saved to.</param>
/// <param name="SizeBytes">The size of the image in bytes.</param>
/// <param name="ContentType">The content type reported by the server, or empty when none was reported.</param>
public sealed record DownloadedWallpaperImage(FileName FileName, string SavedPath, int SizeBytes, string ContentType);
