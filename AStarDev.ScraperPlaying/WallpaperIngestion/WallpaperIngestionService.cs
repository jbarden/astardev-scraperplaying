using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;
using AStarDev.Utilities;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <inheritdoc/>
public class WallpaperIngestionService(IFilesQuery filesQuery, INewWallpaperIngestor newWallpaperIngestor) : IWallpaperIngestionService
{
    /// <inheritdoc/>
    public async Task IngestPageAsync(IReadOnlyList<Data> wallpapers, WallpaperIngestionContext context, IProgress<string> progress, CancellationToken cancellationToken)
    {
        if (wallpapers.Count == 0) return;

        IReadOnlyList<(Data Wallpaper, string Extension)> candidates = [.. wallpapers.Select(wallpaper => (wallpaper, wallpaper.Path.ToFileExtension()))];
        var handles = candidates.Select(candidate => FileHandle.Create(candidate.Wallpaper.Id)).ToList();

        await (await filesQuery.GetExistingHandlesAsync(handles, cancellationToken))
        .Match(
            async existingHandles =>
            {
                var existing = existingHandles.Select(handle => handle.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var (wallpaper, extension) in candidates)
                {
                    if (existing.Contains(wallpaper.Id))
                    {
                        progress.Report($"The file details already exist for wallpaper {wallpaper.Id} - no need to fetch again.");

                        continue;
                    }

                    await newWallpaperIngestor.IngestAsync(wallpaper, extension, context, progress, cancellationToken);
                }
            },
            exception =>
            {
                progress.Report($"Failed to check whether the file details already exist for this page of wallpapers: {exception.Message}");

                return Task.CompletedTask;
            }
        );
    }
}
