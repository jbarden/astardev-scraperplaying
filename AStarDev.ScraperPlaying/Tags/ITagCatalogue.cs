using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.Tags;

/// <summary>Reads the stored tags so they can be edited, and saves the edits.</summary>
public interface ITagCatalogue
{
    /// <summary>Lists a summary of every stored tag, ordered by name.</summary>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    Task<Exceptional<IReadOnlyList<TagSummary>>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Sets whether images carrying each tag are ignored, leaving every other tag as it is.</summary>
    /// <param name="ignoreImageByWallhavenId">The new flag for each changed tag, keyed by its Wallhaven id.</param>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    Task<Exceptional<Unit>> SaveIgnoreImageAsync(IReadOnlyDictionary<int, bool> ignoreImageByWallhavenId, CancellationToken cancellationToken = default);
}
