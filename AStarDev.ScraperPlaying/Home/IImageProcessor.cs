using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
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
    /// <param name="id">The identifier of the image to download.</param>
    /// <param name="path">The local path to save the downloaded image.</param>
    /// <param name="progress">The progress reporter to report the download progress.</param>
    /// <param name="client">The HTTP client used to make the request.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DownloadImageAsync(string id, string path, IProgress<string> progress, HttpClient client, CancellationToken cancellationToken);

    /// <summary>
    /// Processes the image asynchronously based on the provided parameters.
    /// </summary>
    /// <param name="progress">The progress reporter to report the processing progress.</param>
    /// <param name="fileRepository">The repository used to store file entities.</param>
    /// <param name="wallpaper">The wallpaper data to process.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProcessTheImageAsync(IProgress<string> progress, IRepository<FileEntity, FileId> fileRepository, Data wallpaper, CancellationToken cancellationToken);
}