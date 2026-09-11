using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.SearchAPI.SearchResponse;

namespace AStarDev.ScraperPlaying.Home;

/// <summary>
/// Interface for processing images by downloading and handling them asynchronously.
/// </summary>
public interface IImageProcessor
{
    /// <summary>
    /// Downloads an image asynchronously based on the provided parameters.
    /// </summary>
    /// <param name="id">The identifier of the image to download. Also used to name the saved file as "{id}.jpg" in the current working directory.</param>
    /// <param name="imageUri">The remote URL to download the image from.</param>
    /// <param name="progress">The progress reporter to report the download progress.</param>
    /// <param name="client">The HTTP client used to make the request.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DownloadImageAsync(string id, string imageUri, IProgress<string> progress, HttpClient client, CancellationToken cancellationToken);

    /// <summary>
    /// Processes the image asynchronously based on the provided parameters.
    /// </summary>
    /// <param name="fileRepository">The repository used to store file entities.</param>
    /// <param name="wallpaper">The wallpaper data to process.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing an <see cref="Exceptional{T}"/> wrapping the
    /// added <see cref="FileEntity"/> on success, or the captured failure from adding it to
    /// <paramref name="fileRepository"/>.
    /// </returns>
    Task<Exceptional<FileEntity>> ProcessTheImageAsync(IRepository<FileEntity, FileId> fileRepository, Data wallpaper, CancellationToken cancellationToken);
}