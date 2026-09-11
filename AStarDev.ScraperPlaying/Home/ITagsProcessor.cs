using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.Home;

/// <summary>
/// Interface for fetching a wallpaper's detail data (including its assigned tags) and linking those tags to
/// the file, storing each distinct tag only once.
/// </summary>
public interface ITagsProcessor
{
    /// <summary>
    /// Fetches the Wallhaven detail response for the given wallpaper and links each of its tags to
    /// <paramref name="fileId"/>, creating any tag not already stored.
    /// </summary>
    /// <param name="wallpaperId">The Wallhaven wallpaper id to fetch tag detail for.</param>
    /// <param name="fileId">The id of the already-persisted <see cref="FileEntity"/> to link the tags to.</param>
    /// <param name="client">The HTTP client used to make the request.</param>
    /// <param name="progress">The progress reporter to report fetching progress.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing an <see cref="Exceptional{T}"/> indicating
    /// success or the captured failure.
    /// </returns>
    Task<Exceptional<UnitFp>> FetchAndLinkTagsAsync(string wallpaperId, FileId fileId, HttpClient client, IProgress<string> progress, CancellationToken cancellationToken);
}
