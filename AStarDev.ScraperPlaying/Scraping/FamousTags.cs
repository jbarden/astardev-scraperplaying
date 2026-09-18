namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Identifies wallpapers of famous people from their tags.</summary>
public static class FamousTags
{
    private static readonly string[] famousTagFragments = ["actress", "model", "singer"];

    /// <summary>Determines whether any tag's name contains "actress", "model" or "singer" (case-insensitive), alone or as part of a longer tag.</summary>
    /// <param name="tags">The tags of the wallpaper.</param>
    /// <returns><see langword="true"/> when the wallpaper is tagged as a famous person.</returns>
    public static bool AreFamous(IReadOnlyList<WallpaperTag> tags)
        => tags.Any(tag => famousTagFragments.Any(fragment => tag.Name.Contains(fragment, StringComparison.OrdinalIgnoreCase)));
}
