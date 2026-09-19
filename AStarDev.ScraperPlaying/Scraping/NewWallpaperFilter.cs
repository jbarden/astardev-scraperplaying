using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.Scraping;

/// <inheritdoc/>
public class NewWallpaperFilter(IFilesQuery filesQuery) : INewWallpaperFilter
{
    /// <inheritdoc/>
    public async Task<string[]> ExcludeAlreadyDownloadedAsync(string[] wallpaperIds, IProgress<string> progress, CancellationToken cancellationToken)
        => (await filesQuery.GetExistingHandlesAsync([.. wallpaperIds.Select(FileHandle.Create)], cancellationToken))
            .Match(
                existing =>
                {
                    foreach (var wallpaperId in wallpaperIds.Where(id => existing.Contains(FileHandle.Create(id)))) progress.Report($"Wallpaper {wallpaperId} was already downloaded - skipping.");

                    return wallpaperIds.Where(id => !existing.Contains(FileHandle.Create(id))).ToArray();
                },
                exception =>
                {
                    progress.Report($"Failed to check which wallpapers were already downloaded, skipping this page: {exception.Message}");

                    return [];
                });
}
