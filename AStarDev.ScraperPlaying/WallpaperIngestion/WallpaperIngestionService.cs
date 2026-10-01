using System.Diagnostics.CodeAnalysis;
using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;
using AStarDev.Utilities;
using Tag = AStarDev.ScraperPlaying.Scraping.WallhavenResponses.DetailResponse.Tag;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <inheritdoc/>
public sealed class WallpaperIngestionService(IFilesQuery filesQuery, INewWallpaperIngestor newWallpaperIngestor) : IWallpaperIngestionService
{
    /// <inheritdoc/>
    public async Task IngestPageAsync(IReadOnlyList<Data> wallpapers, WallpaperIngestionContext context, IProgress<string> progress, CancellationToken cancellationToken)
    {
        if (wallpapers.Count == 0) return;

        var candidates = BuildCandidates(wallpapers);
        IReadOnlyCollection<FileHandle> handles = [.. candidates.Select(candidate => FileHandle.Create(candidate.Wallpaper.Id))];

        await (await filesQuery.GetExistingHandlesAsync(handles, cancellationToken))
        .Match(
            existingHandles => IngestNewWallpapersAsync(SplitNewCandidates(candidates, existingHandles, progress), context, progress, cancellationToken),
            exception =>
            {
                progress.Report($"Failed to check whether the file details already exist for this page of wallpapers: {exception.Message}");

                return Task.CompletedTask;
            }
        );
    }

    private static IReadOnlyList<WallpaperCandidate> BuildCandidates(IReadOnlyList<Data> wallpapers)
        => [.. wallpapers.Select(wallpaper => new WallpaperCandidate(wallpaper, wallpaper.Path.ToFileExtension()))];

    private static List<WallpaperCandidate> SplitNewCandidates(IReadOnlyList<WallpaperCandidate> candidates, IEnumerable<FileHandle> existingHandles, IProgress<string> progress)
    {
        var existing = existingHandles.Select(handle => handle.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        List<WallpaperCandidate> newWallpapers = [];
        foreach (var candidate in candidates)
        {
            if (existing.Contains(candidate.Wallpaper.Id)) progress.Report($"The file details already exist for wallpaper {candidate.Wallpaper.Id} - no need to fetch again.");
            else newWallpapers.Add(candidate);
        }

        return newWallpapers;
    }

    /// <summary>
    /// Ingests the new wallpapers in page order, fetching the next wallpaper's tags (network only, and one fetch at a time so the API rate limit still applies) while the current one
    /// downloads and is recorded (network and the single, non-thread-safe database context, and one wallpaper at a time).
    /// </summary>
    private async Task IngestNewWallpapersAsync(List<WallpaperCandidate> newWallpapers, WallpaperIngestionContext context, IProgress<string> progress, CancellationToken cancellationToken)
    {
        if (newWallpapers.Count == 0) return;

        var prefetch = newWallpaperIngestor.FetchTagsAsync(newWallpapers[0].Wallpaper, context, progress, cancellationToken);
        try
        {
            for (var i = 0; i < newWallpapers.Count; i++)
            {
                var tags = await prefetch;
                prefetch = i + 1 < newWallpapers.Count ? newWallpaperIngestor.FetchTagsAsync(newWallpapers[i + 1].Wallpaper, context, progress, cancellationToken) : Task.FromResult(Option.None<IReadOnlyList<Tag>>());

                if (tags.TryGetValue(out var fetchedTags)) await newWallpaperIngestor.IngestAsync(newWallpapers[i], fetchedTags, context, progress, cancellationToken);
            }
        }
        finally
        {
            await ObserveAsync(prefetch);
        }
    }

    /// <summary>Waits for an outstanding prefetch that is no longer needed (the loop finished or was abandoned) so it is never left running or unobserved; its outcome is irrelevant.</summary>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The prefetch result is discarded; only its completion matters and any failure it had is already reported by the ingestor.")]
    private static async Task ObserveAsync(Task prefetch)
    {
        try
        {
            await prefetch;
        }
        catch (Exception)
        {
            // Abandoned prefetch (cancellation or failure): nothing to do.
        }
    }
}
