using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.DetailResponse;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Interface for fetching a wallpaper's tags, flagged from the stored tags.</summary>
public interface ITagFetcher
{
    /// <summary>Fetches the Wallhaven detail response for the given wallpaper and returns its tags.</summary>
    /// <param name="wallpaperId">The Wallhaven wallpaper id to fetch tag detail for.</param>
    /// <param name="client">The HTTP client used to make the request.</param>
    /// <param name="personCategories">The tag category names whose tags are famous when the tag has not been stored before.</param>
    /// <param name="progress">The progress reporter to report fetching progress.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, containing an <see cref="Exceptional{T}"/> wrapping the wallpaper's tags, or the captured failure.</returns>
    Task<Exceptional<IReadOnlyList<Tag>>> FetchTagsAsync(string wallpaperId, HttpClient client, IReadOnlyList<string> personCategories, IProgress<string> progress, CancellationToken cancellationToken);
}
