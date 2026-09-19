using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <summary>Interface for downloading and recording the image of a wallpaper scraped from its detail page, using the browser session rather than the JSON API.</summary>
public interface IWallpaperDetailImageProcessor
{
    /// <summary>Downloads the wallpaper's image, records its <see cref="FileEntity"/> and announces the download for the live preview.</summary>
    /// <param name="request">The image to download and record.</param>
    /// <param name="progress">The progress reporter to report download progress.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task containing an <see cref="Exceptional{T}"/> wrapping the added <see cref="FileEntity"/>, or the captured failure.</returns>
    Task<Exceptional<FileEntity>> DownloadAndRecordAsync(ImageDownloadRequest request, IProgress<string> progress, CancellationToken cancellationToken);
}
