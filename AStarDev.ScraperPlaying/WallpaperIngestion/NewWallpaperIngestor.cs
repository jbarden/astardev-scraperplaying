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
    public async Task<Option<IReadOnlyList<Tag>>> FetchTagsAsync(Data wallpaper, WallpaperIngestionContext context, IProgress<string> progress, CancellationToken cancellationToken)
    {
        progress.Report($"No existing data found for wallpaper {wallpaper.Id}.");

        return (await tagFetcher.FetchTagsAsync(wallpaper.Id, context.Client, context.PersonCategories, progress, cancellationToken))
            .Match(
                tags => Option.Some(tags),
                exception =>
                {
                    progress.Report($"Failed to fetch tags for wallpaper {wallpaper.Id}: {exception.Message}");

                    return Option.None<IReadOnlyList<Tag>>();
                });
    }

    /// <inheritdoc/>
    public async Task<IngestOutcome> IngestAsync(WallpaperCandidate candidate, IReadOnlyList<Tag> tags, WallpaperIngestionContext context, IProgress<string> progress, CancellationToken cancellationToken)
    {
        if (tags.Any(tag => tag.IgnoreImage))
        {
            progress.Report($"Ignoring wallpaper {candidate.Wallpaper.Id}: it has a tag flagged to ignore images.");
            RememberIgnored(candidate.Wallpaper.Id, progress);

            return IngestOutcome.Complete;
        }

        return (await Try.RunAsync(() => IngestStepsAsync(candidate, tags, context, progress, cancellationToken)))
            .Match(
                outcome => outcome,
                exception =>
                {
                    progress.Report($"Failed to process image for wallpaper {candidate.Wallpaper.Id}: {exception.Message}");

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

    private async Task<IngestOutcome> IngestStepsAsync(WallpaperCandidate candidate, IReadOnlyList<Tag> tags, WallpaperIngestionContext context, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var request = new WallpaperFileRequest(candidate.Wallpaper, context.Directories.For(tags), WallpaperFileNamer.Create(candidate.Wallpaper.Id, candidate.Extension, tags), context.CategoryLabel);

        var fileEntity = await wallpaperSaver.SaveAsync(request, context, progress, cancellationToken);

        return await LinkTagsAsync(candidate.Wallpaper.Id, fileEntity, tags, context, progress, cancellationToken);
    }

    /// <summary>Links the tags to the recorded file. The file is recorded first, so if linking fails or is cancelled it is discarded: left tracked, the page save would persist an untagged file that later scrapes treat as already ingested.</summary>
    private async Task<IngestOutcome> LinkTagsAsync(string wallpaperId, FileEntity fileEntity, IReadOnlyList<Tag> tags, WallpaperIngestionContext context, IProgress<string> progress, CancellationToken cancellationToken)
    {
        try
        {
            return (await tagLinker.LinkTagsAsync(fileEntity.Id, tags, cancellationToken))
                .Match(
                    _ => IngestOutcome.Complete,
                    exception =>
                    {
                        progress.Report($"Failed to link tags for wallpaper {wallpaperId}: {exception.Message}");
                        Discard(fileEntity, wallpaperId, context, progress);

                        return IngestOutcome.Incomplete;
                    });
        }
        catch (OperationCanceledException)
        {
            Discard(fileEntity, wallpaperId, context, progress);

            throw;
        }
    }

    private static void Discard(FileEntity fileEntity, string wallpaperId, WallpaperIngestionContext context, IProgress<string> progress)
        => _ = context.FileRepository.Delete(fileEntity)
            .Match(
                unit => unit,
                exception =>
                {
                    progress.Report($"Failed to discard the untagged file record for wallpaper {wallpaperId}: {exception.Message}");

                    return Unit.Instance;
                });
}
