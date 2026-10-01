using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <summary>Downloads a wallpaper's image, records it as a file entity and then tells listeners it arrived.</summary>
/// <param name="imageDownloader">Downloads the image.</param>
/// <param name="fileRecorder">Records the downloaded wallpaper as a file entity.</param>
/// <param name="imageDownloadNotifier">Tells listeners an image was downloaded.</param>
public sealed class WallpaperSaver(IImageDownloader imageDownloader, IWallpaperFileRecorder fileRecorder, IImageDownloadNotifier imageDownloadNotifier)
{
    /// <summary>Downloads and records the wallpaper described by <paramref name="request"/>.</summary>
    /// <param name="request">The wallpaper, and the file name and directory its image is saved under.</param>
    /// <param name="context">The client to download with and the repository to record into.</param>
    /// <param name="progress">Receives progress messages.</param>
    /// <param name="cancellationToken">Cancels the download, or the recording before it starts.</param>
    /// <returns>The recorded <see cref="FileEntity"/>; a recording failure is thrown, and listeners are only told of a recorded wallpaper.</returns>
    public async Task<FileEntity> SaveAsync(WallpaperFileRequest request, WallpaperIngestionContext context, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var savedPath = await imageDownloader.DownloadAsync(request, progress, context.Client, cancellationToken);
        progress.Report($"Downloaded image data for wallpaper {request.Wallpaper.Id}");
        cancellationToken.ThrowIfCancellationRequested();

        var entity = fileRecorder.Record(context.FileRepository, request).Match(recorded => recorded, exception => throw exception);
        imageDownloadNotifier.NotifyImageDownloaded(new WallpaperDownloadDetails(savedPath, new WallpaperInfo(request.FileName.Value, request.CategoryLabel, request.Wallpaper.FileSize, request.Wallpaper.DimensionX, request.Wallpaper.DimensionY)));

        return entity;
    }
}
