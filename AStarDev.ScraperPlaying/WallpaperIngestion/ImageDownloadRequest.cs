using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.ScraperPlaying.Scraping;
using Microsoft.Playwright;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <summary>A wallpaper image to download and record.</summary>
/// <param name="Detail">The wallpaper scraped from its detail page.</param>
/// <param name="Page">The browser page whose context is used to make the download request.</param>
/// <param name="Directory">The directory to save the image into.</param>
/// <param name="CategoryLabel">The search category the wallpaper came from, or "Top Wallpapers" when it did not come from a specific search category.</param>
/// <param name="FileRepository">The repository used to store the file entity.</param>
public sealed record ImageDownloadRequest(WallpaperDetail Detail, IPage Page, string Directory, string CategoryLabel, IRepository<FileEntity, FileId> FileRepository);
