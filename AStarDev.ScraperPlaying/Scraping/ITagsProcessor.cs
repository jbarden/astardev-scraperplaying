using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Interface for linking a wallpaper's tags to its file, storing each distinct tag only once.</summary>
public interface ITagsProcessor
{
    /// <summary>Links each of the given tags to <paramref name="fileId"/>, creating any tag not already stored and reusing any that is - regardless of where the tags were fetched or scraped from.</summary>
    /// <param name="fileId">The id of the already-persisted <see cref="FileEntity"/> to link the tags to.</param>
    /// <param name="tags">The tags to link. Duplicates by <see cref="WallpaperTag.WallhavenTagId"/> are linked only once.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, containing an <see cref="Exceptional{T}"/> indicating success or the captured failure.</returns>
    Task<Exceptional<Unit>> LinkTagsAsync(FileId fileId, IReadOnlyList<WallpaperTag> tags, CancellationToken cancellationToken);

    /// <summary>Marks the tags created since the last call as saved, so later wallpapers can reuse them without looking them up again. Call after the changes made by <see cref="LinkTagsAsync"/> were saved.</summary>
    void AcceptPendingTags();

    /// <summary>Forgets the tags created since the last accept, because the changes that would have stored them were discarded. Tags that already existed in the database stay cached.</summary>
    void DiscardPendingTags();
}
