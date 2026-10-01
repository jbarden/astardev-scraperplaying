using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.DetailResponse;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <summary>The directories a page of wallpapers can be saved into: the normal one, and the one for wallpapers carrying a famous tag.</summary>
/// <param name="Directory">The directory for wallpapers with no famous tag.</param>
/// <param name="FamousDirectory">The directory for wallpapers with at least one famous tag.</param>
public sealed record SaveDirectories(string Directory, string FamousDirectory)
{
    /// <summary>Gets the directory a wallpaper with the specified tags is saved into.</summary>
    /// <param name="tags">The wallpaper's tags.</param>
    public string For(IReadOnlyList<Tag> tags) => tags.Any(tag => tag.IsFamous) ? FamousDirectory : Directory;
}
