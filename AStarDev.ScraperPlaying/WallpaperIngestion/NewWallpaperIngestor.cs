using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;
using Tag = AStarDev.ScraperPlaying.Scraping.WallhavenResponses.DetailResponse.Tag;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <inheritdoc/>
public sealed class NewWallpaperIngestor(ITagsProcessor tagsProcessor, IImageDownloader imageDownloader, IWallpaperFileRecorder fileRecorder, IImageDownloadNotifier imageDownloadNotifier) : INewWallpaperIngestor
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<Tag>> FetchTagsAsync(Data wallpaper, WallpaperIngestionContext context, IProgress<string> progress, CancellationToken cancellationToken)
    {
        progress.Report($"No existing data found for wallpaper {wallpaper.Id}.");

        return (await tagsProcessor.FetchTagsAsync(wallpaper.Id, context.Client, context.PersonCategories, progress, cancellationToken))
            .Match(
                tags => tags,
                exception =>
                {
                    progress.Report($"Failed to fetch tags for wallpaper {wallpaper.Id}: {exception.Message}");

                    return [];
                });
    }

    /// <inheritdoc/>
    public async Task IngestAsync(WallpaperCandidate candidate, IReadOnlyList<Tag> tags, WallpaperIngestionContext context, IProgress<string> progress, CancellationToken cancellationToken)
    {
        if (tags.Any(tag => tag.IgnoreImage))
        {
            progress.Report($"Ignoring wallpaper {candidate.Wallpaper.Id}: it has a tag flagged to ignore images.");

            return;
        }

        await Try.RunAsync(() => IngestStepsAsync(candidate, tags, context, progress, cancellationToken))
            .MatchAsync(
                _ => Task.CompletedTask,
                exception =>
                {
                    progress.Report($"Failed to process image for wallpaper {candidate.Wallpaper.Id}: {exception.Message}");

                    return Unit.Instance;
                });
    }

    private async Task<Unit> IngestStepsAsync(WallpaperCandidate candidate, IReadOnlyList<Tag> tags, WallpaperIngestionContext context, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var request = new WallpaperFileRequest(candidate.Wallpaper, context.Directories.For(tags), WallpaperFileNamer.Create(candidate.Wallpaper.Id, candidate.Extension, tags), context.CategoryLabel);

        await DownloadAsync(request, context.Client, progress, cancellationToken);
        var fileEntity = Record(request, context, cancellationToken);
        await LinkTagsAsync(candidate.Wallpaper.Id, fileEntity.Id, tags, progress, cancellationToken);

        return Unit.Instance;
    }

    private async Task DownloadAsync(WallpaperFileRequest request, HttpClient client, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var savedPath = await imageDownloader.DownloadAsync(request, progress, client, cancellationToken);
        progress.Report($"Downloaded image data for wallpaper {request.Wallpaper.Id}");
        imageDownloadNotifier.NotifyImageDownloaded(new WallpaperDownloadDetails(savedPath, new WallpaperInfo(request.FileName.Value, request.CategoryLabel, request.Wallpaper.FileSize, request.Wallpaper.DimensionX, request.Wallpaper.DimensionY)));
    }

    private FileEntity Record(WallpaperFileRequest request, WallpaperIngestionContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return fileRecorder.Record(context.FileRepository, request).Match(entity => entity, exception => throw exception);
    }

    private async Task LinkTagsAsync(string wallpaperId, FileId fileId, IReadOnlyList<Tag> tags, IProgress<string> progress, CancellationToken cancellationToken)
        => await tagsProcessor.LinkTagsAsync(fileId, tags, cancellationToken)
            .MatchAsync(
                _ => Task.CompletedTask,
                exception =>
                {
                    progress.Report($"Failed to link tags for wallpaper {wallpaperId}: {exception.Message}");

                    return Unit.Instance;
                });
}
