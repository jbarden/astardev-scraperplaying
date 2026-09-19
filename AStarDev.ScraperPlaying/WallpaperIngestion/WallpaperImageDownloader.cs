using System.IO.Abstractions;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.Utilities;
using Microsoft.Playwright;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <inheritdoc/>
public class WallpaperImageDownloader(IFileSystem fileSystem) : IWallpaperImageDownloader
{
    /// <inheritdoc/>
    public Task<Exceptional<DownloadedWallpaperImage>> DownloadAsync(WallpaperDetail detail, IPage page, string directory, IProgress<string> progress, CancellationToken cancellationToken)
        => Try.RunAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(detail.ImageUrl)) throw new InvalidOperationException($"No image URL was found on the detail page for wallpaper {detail.WallpaperId}.");

            var fileName = new FileName($"{detail.WallpaperId}{detail.ImageUrl.ToFileExtension()}");

            progress.Report($"Downloading image for wallpaper {detail.WallpaperId} from {detail.ImageUrl}");
            var response = await page.Context.APIRequest.GetAsync(detail.ImageUrl).WaitAsync(cancellationToken);
            if (!response.Ok) throw new HttpRequestException($"Downloading the image for wallpaper {detail.WallpaperId} failed with status {response.Status}.");

            var body = await response.BodyAsync().WaitAsync(cancellationToken);
            fileSystem.Directory.CreateDirectory(directory);
            var savedPath = fileSystem.Path.Combine(directory, fileName.Value);
            await fileSystem.File.WriteAllBytesAsync(savedPath, body, cancellationToken);

            return new DownloadedWallpaperImage(fileName, savedPath, body.Length, response.Headers.GetValueOrDefault("content-type", string.Empty));
        }, cancellationToken);
}
