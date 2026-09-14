using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;

namespace AStarDev.ScraperPlaying.Home;

/// <summary>The per-page state a wallpaper ingestion needs, unchanged across every wallpaper on the page.</summary>
/// <param name="Directory">The directory to save wallpaper images into.</param>
/// <param name="Client">The HTTP client used to make requests.</param>
/// <param name="FileRepository">The repository used to store wallpapers' file entities.</param>
/// <param name="CategoryLabel">The search category being ingested, or "Top Wallpapers" when it is not a specific search category.</param>
public sealed record WallpaperIngestionContext(string Directory, HttpClient Client, IRepository<FileEntity, FileId> FileRepository, string CategoryLabel);
