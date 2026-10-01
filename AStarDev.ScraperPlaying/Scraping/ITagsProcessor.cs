using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.DetailResponse;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Interface for fetching a wallpaper's tags and linking them to its file, storing each distinct tag only once.</summary>
public interface ITagsProcessor
{
    /// <summary>Fetches the Wallhaven detail response for the given wallpaper and returns its tags.</summary>
    /// <param name="wallpaperId">The Wallhaven wallpaper id to fetch tag detail for.</param>
    /// <param name="client">The HTTP client used to make the request.</param>
    /// <param name="personCategories">The tag category names whose tags are famous when the tag has not been stored before.</param>
    /// <param name="progress">The progress reporter to report fetching progress.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, containing an <see cref="Exceptional{T}"/> wrapping the wallpaper's tags, or the captured failure.</returns>
    Task<Exceptional<IReadOnlyList<Tag>>> FetchTagsAsync(string wallpaperId, HttpClient client, IReadOnlyList<string> personCategories, IProgress<string> progress, CancellationToken cancellationToken);

    /// <summary>Links each of the tags to <paramref name="fileId"/>, creating any tag not already stored.</summary>
    /// <param name="fileId">The id of the already-persisted <see cref="FileEntity"/> to link the tags to.</param>
    /// <param name="tags">The tags to link, as returned by <see cref="FetchTagsAsync"/>.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, containing an <see cref="Exceptional{T}"/> indicating success or the captured failure.</returns>
    Task<Exceptional<Unit>> LinkTagsAsync(FileId fileId, IReadOnlyList<Tag> tags, CancellationToken cancellationToken);
}
