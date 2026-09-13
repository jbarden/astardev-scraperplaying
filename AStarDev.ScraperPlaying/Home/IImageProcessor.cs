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
    /// <param name="id">The identifier of the image to download. Also used to name the saved file as "{id}{extension}".</param>
    /// <param name="imageUri">The remote URL to download the image from.</param>
    /// <param name="extension">The file extension (including the leading '.') to save the file with.</param>
    /// <param name="directory">The directory to save the downloaded file into, created if it does not already exist.</param>
    /// <param name="progress">The progress reporter to report the download progress.</param>
    /// <param name="client">The HTTP client used to make the request.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DownloadImageAsync(string id, string imageUri, string extension, string directory, IProgress<string> progress, HttpClient client, CancellationToken cancellationToken);

    /// <summary>
    /// Processes the image asynchronously based on the provided parameters.
    /// </summary>
    /// <param name="fileRepository">The repository used to store file entities.</param>
    /// <param name="wallpaper">The wallpaper data to process.</param>
    /// <param name="directory">The directory the wallpaper's image was saved into.</param>
    /// <param name="extension">The file extension (including the leading '.') the wallpaper's image was saved with.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing an <see cref="Exceptional{T}"/> wrapping the
    /// added <see cref="FileEntity"/> on success, or the captured failure from adding it to
    /// <paramref name="fileRepository"/>.
    /// </returns>
    Task<Exceptional<FileEntity>> ProcessTheImageAsync(IRepository<FileEntity, FileId> fileRepository, Data wallpaper, string directory, string extension, CancellationToken cancellationToken);
}