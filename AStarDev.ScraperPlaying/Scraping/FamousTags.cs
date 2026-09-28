namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Identifies wallpapers of famous people from their tags.</summary>
public static class FamousTags
{
    private static readonly string[] famousTagFragments = ["actress", "model", "singer"];

    /// <summary>Determines whether any tag's name contains "actress", "model" or "singer" (case-insensitive), alone or as part of a longer tag.</summary>
    /// <param name="tags">The tags of the wallpaper.</param>
    /// <returns><see langword="true"/> when the wallpaper is tagged as a famous person.</returns>
    public static bool AreFamous(IReadOnlyList<WallpaperTag> tags)
        => tags.Any(IsFamous);

    /// <summary>Determines whether the tag's name contains "actress", "model" or "singer" (case-insensitive).</summary>
    /// <param name="tag">The tag to check.</param>
    /// <returns><see langword="true"/> when the tag marks a famous person.</returns>
    public static bool IsFamous(WallpaperTag tag)
        => famousTagFragments.Any(fragment => tag.Name.Contains(fragment, StringComparison.OrdinalIgnoreCase));
}
