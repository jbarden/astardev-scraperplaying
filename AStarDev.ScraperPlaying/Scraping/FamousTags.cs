namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Identifies wallpapers of famous people from their tags.</summary>
public static class FamousTags
{
    private static readonly string[] personCategories = ["Celebrities", "Models", "Pornstars", "Other Figures", "Photographers"];
    private static readonly string[] famousTagFragments = ["actress", "model", "singer"];

    /// <summary>Determines whether any tag's name contains "actress", "model" or "singer" (case-insensitive), alone or as part of a longer tag, or any tag is a person's name (see <see cref="IsPersonName"/>).</summary>
    /// <param name="tags">The tags of the wallpaper.</param>
    /// <returns><see langword="true"/> when the wallpaper is tagged as a famous person.</returns>
    public static bool AreFamous(IReadOnlyList<WallpaperTag> tags)
        => tags.Any(tag => IsFamous(tag) || IsPersonName(tag));

    /// <summary>Determines whether the tag's name contains "actress", "model" or "singer" (case-insensitive).</summary>
    /// <param name="tag">The tag to check.</param>
    /// <returns><see langword="true"/> when the tag marks a famous person.</returns>
    public static bool IsFamous(WallpaperTag tag)
        => famousTagFragments.Any(fragment => tag.Name.Contains(fragment, StringComparison.OrdinalIgnoreCase));

    /// <summary>Determines whether the tag is a person's name: it is in one of the person categories (Celebrities, Models, Pornstars, Other Figures or Photographers) and its name starts with an upper-case letter, which rules out descriptive tags such as "finger pointing" that share those categories.</summary>
    /// <param name="tag">The tag to check.</param>
    /// <returns><see langword="true"/> when the tag is a person's name.</returns>
    public static bool IsPersonName(WallpaperTag tag)
        => tag.Name.Length > 0 && char.IsUpper(tag.Name[0]) && personCategories.Contains(tag.Category, StringComparer.OrdinalIgnoreCase);
}
