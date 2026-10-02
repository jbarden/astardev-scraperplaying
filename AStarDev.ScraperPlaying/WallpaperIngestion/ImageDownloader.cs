using System.IO.Abstractions;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <inheritdoc/>
/// <param name="fileSystem">The file system the image is written to.</param>
/// <param name="timeProvider">The time source used to wait before each download.</param>
/// <param name="pacing">The range of the random wait applied before each download.</param>
public sealed class ImageDownloader(IFileSystem fileSystem, TimeProvider timeProvider, DownloadPacing pacing) : IImageDownloader
{
    private const string PartialFileExtension = ".part";

    /// <inheritdoc/>
    public async Task<string> DownloadAsync(WallpaperFileRequest request, IProgress<string> progress, HttpClient client, CancellationToken cancellationToken)
    {
        await Task.Delay(pacing.NextDelay(), timeProvider, cancellationToken);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, request.Wallpaper.Path);

        using var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        _ = response.EnsureSuccessStatusCode();
        progress.Report($"Downloading image for wallpaper {request.Wallpaper.Id} from {request.Wallpaper.Path}");
        using Stream downloadStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        _ = fileSystem.Directory.CreateDirectory(request.Directory);
        var savedPath = fileSystem.Path.Combine(request.Directory, request.FileName.Value);
        var partialPath = $"{savedPath}{PartialFileExtension}";
        try
        {
            using (var fileStream = fileSystem.FileStream.New(partialPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await downloadStream.CopyToAsync(fileStream, cancellationToken);
            }

            fileSystem.File.Move(partialPath, savedPath, overwrite: true);
        }
        finally
        {
            // Only reached with the partial file still present when the download failed or was cancelled: never leave a truncated image in the folder.
            if (fileSystem.File.Exists(partialPath)) fileSystem.File.Delete(partialPath);
        }

        return savedPath;
    }
}
