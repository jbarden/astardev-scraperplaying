using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <inheritdoc/>
public class WallpaperDetailImageProcessor(IWallpaperImageDownloader imageDownloader, IWallpaperFileRecorder fileRecorder, IImageDownloadNotifier imageDownloadNotifier) : IWallpaperDetailImageProcessor
{
    /// <inheritdoc/>
    public async Task<Exceptional<FileEntity>> DownloadAndRecordAsync(ImageDownloadRequest request, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var downloaded = await imageDownloader.DownloadAsync(request.Detail, request.Page, request.Directory, progress, cancellationToken);

        return downloaded.Match(
            image => fileRecorder.Record(request.Detail, image, request.Directory, request.FileRepository).Match(
                file =>
                {
                    imageDownloadNotifier.NotifyImageDownloaded(new WallpaperDownloadDetails(image.SavedPath, request.Detail.WallpaperId, request.CategoryLabel, image.SizeBytes, request.Detail.DimensionX, request.Detail.DimensionY));

                    return Exceptional.Success(file);
                },
                Exceptional.Failure<FileEntity>),
            Exceptional.Failure<FileEntity>);
    }
}
