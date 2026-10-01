namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>
/// Holds the <see cref="TagFlagCache"/> for one scrape run (Scoped) so <see cref="TagFetcher"/> and <see cref="TagLinker"/> share it.
/// The first fetch always completes before any other database work starts, so the load never overlaps another use of the database context.
/// </summary>
public sealed class TagFlagStore
{
    /// <summary>Gets the loaded cache, or <see cref="TagFlagCache.Empty"/> until <see cref="Load"/> is called.</summary>
    public TagFlagCache Cache { get; private set; } = TagFlagCache.Empty;

    /// <summary>Gets a value indicating whether the cache has been loaded.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>Stores the cache loaded for this run.</summary>
    /// <param name="cache">The loaded cache.</param>
    public void Load(TagFlagCache cache)
    {
        Cache = cache;
        IsLoaded = true;
    }
}
