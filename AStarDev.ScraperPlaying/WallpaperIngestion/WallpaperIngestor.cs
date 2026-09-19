using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <inheritdoc/>
public class WallpaperIngestor(IWallpaperDetailPageScraper detailPageScraper, ISaveDirectoryResolver saveDirectoryResolver, IWallpaperDetailImageProcessor imageProcessor, ITagsProcessor tagsProcessor, Func<TimeSpan> pacingDelay) : IWallpaperIngestor
{
    /// <inheritdoc/>
    public async Task IngestAsync(string wallpaperId, PageIngestionContext context, IProgress<string> progress, CancellationToken cancellationToken)
    {
        await ScrapeAndDownloadAsync(wallpaperId, context, progress, cancellationToken);
        await Task.Delay(pacingDelay(), cancellationToken);
    }

    private async Task ScrapeAndDownloadAsync(string wallpaperId, PageIngestionContext context, IProgress<string> progress, CancellationToken cancellationToken)
        => await (await Try.RunAsync(() => detailPageScraper.ScrapeAsync(wallpaperId, context.Page, context.BaseUrl, progress, cancellationToken), cancellationToken))
            .Match(
                async detail =>
                {
                    await Task.Delay(pacingDelay(), cancellationToken);

                    await (await Try.RunAsync(() => saveDirectoryResolver.ResolveSaveDirectoryAsync(context.CategoryName, FamousTags.AreFamous(detail.Tags), cancellationToken), cancellationToken))
                        .Match(
                            directory => DownloadAndLinkAsync(detail, directory, context, progress, cancellationToken),
                            exception =>
                            {
                                progress.Report($"Failed to resolve the save directory for wallpaper {wallpaperId}: {exception.Message}");

                                return Task.CompletedTask;
                            });
                },
                exception =>
                {
                    progress.Report($"Failed to scrape wallpaper {wallpaperId}: {exception.Message}");

                    return Task.CompletedTask;
                });

    private async Task DownloadAndLinkAsync(WallpaperDetail detail, string directory, PageIngestionContext context, IProgress<string> progress, CancellationToken cancellationToken)
        => await (await imageProcessor.DownloadAndRecordAsync(new ImageDownloadRequest(detail, context.Page, directory, context.CategoryLabel, context.FileRepository), progress, cancellationToken))
            .Match(
                file => LinkTagsAsync(file, detail, progress, cancellationToken),
                exception =>
                {
                    progress.Report($"Failed to process image for wallpaper {detail.WallpaperId}: {exception.Message}");

                    return Task.CompletedTask;
                });

    private async Task LinkTagsAsync(FileEntity file, WallpaperDetail detail, IProgress<string> progress, CancellationToken cancellationToken)
        => (await tagsProcessor.LinkTagsAsync(file.Id, detail.Tags, cancellationToken))
            .Match(
                _ => Unit.Instance,
                exception =>
                {
                    progress.Report($"Failed to link tags for wallpaper {detail.WallpaperId}: {exception.Message}");

                    return Unit.Instance;
                });
}
