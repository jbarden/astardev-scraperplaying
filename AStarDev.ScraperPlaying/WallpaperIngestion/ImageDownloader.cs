using System.IO.Abstractions;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <inheritdoc/>
public sealed class ImageDownloader(IFileSystem fileSystem) : IImageDownloader
{
    /// <inheritdoc/>
    public async Task<string> DownloadAsync(WallpaperFileRequest request, IProgress<string> progress, HttpClient client, CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, request.Wallpaper.Path);

        using var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        _ = response.EnsureSuccessStatusCode();
        progress.Report($"Downloading image for wallpaper {request.Wallpaper.Id} from {request.Wallpaper.Path}");
        using Stream downloadStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        _ = fileSystem.Directory.CreateDirectory(request.Directory);
        var savedPath = fileSystem.Path.Combine(request.Directory, request.FileName.Value);
        using (var fileStream = fileSystem.FileStream.New(savedPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await downloadStream.CopyToAsync(fileStream, cancellationToken);
        }

        return savedPath;
    }
}
