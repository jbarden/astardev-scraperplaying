using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using Microsoft.Playwright;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <summary>The state shared by every wallpaper ingested during one search.</summary>
/// <param name="Page">The browser page used to visit detail pages and download images.</param>
/// <param name="BaseUrl">The base URL of the site being scraped.</param>
/// <param name="CategoryName">The search category name, or <see cref="Option{T}.None"/> for the "Top Wallpapers" scrape.</param>
/// <param name="CategoryLabel">The search category label, or "Top Wallpapers" when it is not a specific search category.</param>
/// <param name="FileRepository">The repository used to store the wallpapers' file entities.</param>
public sealed record PageIngestionContext(IPage Page, Uri BaseUrl, Option<string> CategoryName, string CategoryLabel, IRepository<FileEntity, FileId> FileRepository);
