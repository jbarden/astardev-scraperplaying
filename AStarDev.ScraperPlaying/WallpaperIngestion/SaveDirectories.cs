using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.DetailResponse;
using AStarDev.Utilities;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <summary>Where a page of wallpapers can be saved: the normal or the famous root, the names of the wallpaper's name tags, and the category being scraped.</summary>
/// <param name="Root">The root directory for wallpapers with no famous tag.</param>
/// <param name="FamousRoot">The root directory for wallpapers with at least one famous tag.</param>
/// <param name="CategorySegment">The directory segment for the search category, or for the top wallpapers.</param>
public sealed record SaveDirectories(string Root, string FamousRoot, string CategorySegment)
{
    private const int MaxNameSegmentLength = 100;
    private const string InvalidPathCharacters = "\\/:*?\"<>|";

    /// <summary>Gets the directory a wallpaper with the specified tags is saved into: the famous root when any tag is famous, otherwise the normal root; then the names of its name tags, slugged and joined with underscores, when it has any; then the category.</summary>
    /// <param name="tags">The wallpaper's tags.</param>
    public string For(IReadOnlyList<Tag> tags)
    {
        var root = tags.Any(tag => tag.IsFamous) ? FamousRoot : Root;
        var names = string.Join('_', tags.Where(tag => tag.IsName).Select(tag => NameSegment(tag.Name)).Where(name => name.Length > 0));

        return Path.Combine(root, TrimToLimit(names), CategorySegment);
    }

    private static string NameSegment(string name)
        => string.Concat(name.ToDirectorySlug().Where(character => !InvalidPathCharacters.Contains(character, StringComparison.Ordinal) && !char.IsControl(character))).Trim('.');

    private static string TrimToLimit(string names)
        => names.Length > MaxNameSegmentLength ? names[..MaxNameSegmentLength] : names;
}
