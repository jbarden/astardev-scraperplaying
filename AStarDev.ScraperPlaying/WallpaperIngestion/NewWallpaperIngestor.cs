using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;
using Tag = AStarDev.ScraperPlaying.Scraping.WallhavenResponses.DetailResponse.Tag;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <inheritdoc/>
public sealed class NewWallpaperIngestor(ITagFetcher tagFetcher, ITagLinker tagLinker, WallpaperSaver wallpaperSaver, IIgnoredWallpapers ignoredWallpapers) : INewWallpaperIngestor
{
    /// <inheritdoc/>
    public async Task<Option<IReadOnlyList<Tag>>> FetchTagsAsync(Data wallpaper, IngestionRun run)
    {
        run.Progress.Report($"No existing data found for wallpaper {wallpaper.Id}.");

        return (await tagFetcher.FetchTagsAsync(wallpaper.Id, run))
            .Match(
                tags => Option.Some(tags),
                exception =>
                {
                    run.Progress.Report($"Failed to fetch tags for wallpaper {wallpaper.Id}: {exception.Message}");

                    return Option.None<IReadOnlyList<Tag>>();
                });
    }

    /// <inheritdoc/>
    public async Task<IngestOutcome> IngestAsync(WallpaperCandidate candidate, IReadOnlyList<Tag> tags, IngestionRun run)
    {
        if (tags.Any(tag => tag.IgnoreImage))
        {
            run.Progress.Report($"Ignoring wallpaper {candidate.Wallpaper.Id}: it has a tag flagged to ignore images.");
            RememberIgnored(candidate.Wallpaper.Id, run.Progress);

            return IngestOutcome.Complete;
        }

        return (await Try.RunAsync(() => IngestStepsAsync(candidate, tags, run)))
            .Match(
                outcome => outcome,
                exception =>
                {
                    run.Progress.Report($"Failed to process image for wallpaper {candidate.Wallpaper.Id}: {exception.Message}");

                    return IngestOutcome.Incomplete;
                });
    }

    /// <summary>Remembers the ignored wallpaper so later scrapes skip it before fetching its tags. A failure only costs those later fetches, so it is reported and the wallpaper is still ignored.</summary>
    private void RememberIgnored(string wallpaperId, IProgress<string> progress)
        => _ = ignoredWallpapers.Record(FileHandle.Create(wallpaperId))
            .Match(
                unit => unit,
                exception =>
                {
                    progress.Report($"Failed to remember that wallpaper {wallpaperId} is ignored: {exception.Message}");

                    return Unit.Instance;
                });

    private async Task<IngestOutcome> IngestStepsAsync(WallpaperCandidate candidate, IReadOnlyList<Tag> tags, IngestionRun run)
    {
        var request = new WallpaperFileRequest(candidate.Wallpaper, run.Context.Directories.For(tags), WallpaperFileNamer.Create(candidate.Wallpaper.Id, candidate.Extension, tags), run.Context.CategoryLabel);

        var fileEntity = await wallpaperSaver.SaveAsync(request, run.Context, run.Progress, run.CancellationToken);

        return await LinkTagsAsync(candidate.Wallpaper.Id, fileEntity, tags, run);
    }

    /// <summary>Links the tags to the recorded file. The file is recorded first, so if linking fails or is cancelled it is discarded: left tracked, the page save would persist an untagged file that later scrapes treat as already ingested.</summary>
    private async Task<IngestOutcome> LinkTagsAsync(string wallpaperId, FileEntity fileEntity, IReadOnlyList<Tag> tags, IngestionRun run)
    {
        try
        {
            return (await tagLinker.LinkTagsAsync(fileEntity.Id, tags, run.CancellationToken))
                .Match(
                    _ => IngestOutcome.Complete,
                    exception =>
                    {
                        run.Progress.Report($"Failed to link tags for wallpaper {wallpaperId}: {exception.Message}");
                        Discard(fileEntity, wallpaperId, run);

                        return IngestOutcome.Incomplete;
                    });
        }
        catch (OperationCanceledException)
        {
            Discard(fileEntity, wallpaperId, run);

            throw;
        }
    }

    private static void Discard(FileEntity fileEntity, string wallpaperId, IngestionRun run)
        => _ = run.Context.FileRepository.Delete(fileEntity)
            .Match(
                unit => unit,
                exception =>
                {
                    run.Progress.Report($"Failed to discard the untagged file record for wallpaper {wallpaperId}: {exception.Message}");

                    return Unit.Instance;
                });
}
