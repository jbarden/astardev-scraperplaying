using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.WallpaperIngestion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>
/// Fetches wallhaven.cc search-result pages by driving a real browser via Playwright instead of
/// calling the JSON API. For each wallpaper found on a listing page it visits the wallpaper's own
/// detail page, downloads the image, records the file, and links its tags.
/// </summary>
/// <inheritdoc/>
public class WebsitePagesProcessor(IPlaywrightBrowserSession browserSession, IWallpaperDetailPageScraper detailPageScraper, IWallpaperDetailImageProcessor imageProcessor, ITagsProcessor tagsProcessor, ISaveDirectoryResolver saveDirectoryResolver, IUnitOfWork unitOfWork, Func<TimeSpan> pacingDelay) : IPagesProcessor
{
    private const int MaxPagesPerSearch = 4;

    /// <inheritdoc/>
    public async Task FetchAndProcessPagesAsync(string logLabel, Option<string> categoryName, Func<int, string> pageUrlFactory, WallhavenConnection connection, IProgress<string> progress, CancellationToken cancellationToken)
    {
        try
        {
            var page = await browserSession.GetPageAsync(connection.UseHeadless, cancellationToken);
            var fileRepository = unitOfWork.GetRepository<FileEntity, FileId>();
            var categoryLabel = categoryName.Match(name => name, () => "Top Wallpapers");
            var pageNumber = 1;
            await Task.Delay(pacingDelay(), cancellationToken);

            int wallpaperCount;
            do
            {
                var wallpaperIds = await FetchPageAsync(logLabel, pageUrlFactory, pageNumber, page, connection.BaseUrl, progress, cancellationToken);
                wallpaperCount = wallpaperIds?.Length ?? 0;
                progress.Report($"Found {wallpaperCount} wallpaper(s) on {logLabel} page {pageNumber}.");

                foreach (var wallpaperId in wallpaperIds ?? [])
                {
                    await IngestWallpaperAsync(wallpaperId, page, connection.BaseUrl, categoryName, categoryLabel, fileRepository, progress, cancellationToken);
                    await Task.Delay(pacingDelay(), cancellationToken);
                }

                await unitOfWork.SaveChangesAsync(cancellationToken);
                await Task.Delay(pacingDelay(), cancellationToken);
                pageNumber++;
            } while (wallpaperCount > 0 && pageNumber <= MaxPagesPerSearch);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            progress.Report("Scrape cancelled.");
            await SavePartiallyIngestedPageAsync(progress);

            throw;
        }
        catch (PlaywrightException ex)
        {
            progress.Report($"An error occurred navigating the website during {logLabel}: {ex.Message}");

            throw;
        }
    }

    private async Task IngestWallpaperAsync(string wallpaperId, IPage page, Uri baseUrl, Option<string> categoryName, string categoryLabel, IRepository<FileEntity, FileId> fileRepository, IProgress<string> progress, CancellationToken cancellationToken)
        => await (await Try.RunAsync(() => detailPageScraper.ScrapeAsync(wallpaperId, page, baseUrl, progress, cancellationToken), cancellationToken))
            .Match(
                async detail =>
                {
                    await Task.Delay(pacingDelay(), cancellationToken);

                    await (await Try.RunAsync(() => saveDirectoryResolver.ResolveSaveDirectoryAsync(categoryName, FamousTags.AreFamous(detail.Tags), cancellationToken), cancellationToken))
                        .Match(
                            directory => DownloadAndLinkAsync(wallpaperId, detail, page, directory, categoryLabel, fileRepository, progress, cancellationToken),
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

    private async Task DownloadAndLinkAsync(string wallpaperId, WallpaperDetail detail, IPage page, string directory, string categoryLabel, IRepository<FileEntity, FileId> fileRepository, IProgress<string> progress, CancellationToken cancellationToken)
        => await (await imageProcessor.DownloadAndRecordAsync(detail, page, directory, categoryLabel, fileRepository, progress, cancellationToken))
            .Match(
                recorded => recorded.Match(
                    file => LinkTagsAsync(wallpaperId, file, detail, progress, cancellationToken),
                    () => Task.CompletedTask),
                exception =>
                {
                    progress.Report($"Failed to process image for wallpaper {wallpaperId}: {exception.Message}");

                    return Task.CompletedTask;
                });

    private async Task LinkTagsAsync(string wallpaperId, FileEntity file, WallpaperDetail detail, IProgress<string> progress, CancellationToken cancellationToken)
        => (await tagsProcessor.LinkTagsAsync(file.Id, detail.Tags, cancellationToken))
            .Match(
                _ => Unit.Instance,
                exception =>
                {
                    progress.Report($"Failed to link tags for wallpaper {wallpaperId}: {exception.Message}");

                    return Unit.Instance;
                });

    private async Task SavePartiallyIngestedPageAsync(IProgress<string> progress)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            progress.Report("Scrape cancelled - saved wallpapers downloaded so far this page.");
        }
        catch (DbUpdateException ex)
        {
            progress.Report($"Scrape cancelled - failed to save wallpapers downloaded so far this page: {ex.Message}");
        }
    }

    private static async Task<string[]?> FetchPageAsync(string logLabel, Func<int, string> pageUrlFactory, int pageNumber, IPage page, Uri baseUrl, IProgress<string> progress, CancellationToken cancellationToken)
    {
        progress.Report($"Navigating to {logLabel} page {pageNumber}.");
        var targetUrl = new Uri(baseUrl, pageUrlFactory(pageNumber));

        await page.GotoAsync(targetUrl.ToString(), new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded }).WaitAsync(cancellationToken);

        var wallpaperIds = await page.Locator("figure.thumb")
            .EvaluateAllAsync<string[]>("elements => elements.map(element => element.dataset.wallpaperId)")
            .WaitAsync(cancellationToken);

        progress.Report(wallpaperIds.Length == 0
            ? $"No wallpapers found on {logLabel} page {pageNumber}."
            : $"Found {wallpaperIds.Length} wallpaper(s) on {logLabel} page {pageNumber}: {string.Join(", ", wallpaperIds)}.");

        return wallpaperIds;
    }
}
