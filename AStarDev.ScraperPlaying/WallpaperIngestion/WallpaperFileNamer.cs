using AStarDev.ControlDb.FileDetail;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.Utilities;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <summary>Generates the file name a wallpaper is saved and recorded under.</summary>
public static class WallpaperFileNamer
{
    private const int maxPrefixLength = 100;
    private const string invalidFileNameCharacters = "\\/:*?\"<>|";

    /// <summary>Generates the file name for a wallpaper. Wallpapers tagged as a famous person (see <see cref="FamousTags"/>) are prefixed with their other tag names, joined by underscores; all others use just the wallpaper id.</summary>
    /// <param name="detail">The wallpaper scraped from its detail page.</param>
    /// <returns>The file name, including the extension taken from the image URL.</returns>
    public static FileName Create(WallpaperDetail detail)
    {
        var prefix = CreatePrefix(detail.Tags);
        var baseName = prefix.Length == 0 ? detail.WallpaperId : $"{prefix}_{detail.WallpaperId}";

        return new FileName($"{baseName}{detail.ImageUrl.ToFileExtension()}");
    }

    private static string CreatePrefix(IReadOnlyList<WallpaperTag> tags)
    {
        if (!FamousTags.AreFamous(tags)) return string.Empty;

        var joined = string.Join('_', tags.Where(tag => !FamousTags.IsFamous(tag)).Select(tag => Sanitise(tag.Name)).Where(name => name.Length > 0));

        return joined.Length > maxPrefixLength ? joined[..maxPrefixLength] : joined;
    }

    private static string Sanitise(string tagName)
        => new([.. tagName.Trim().Replace(' ', '_').Where(character => !invalidFileNameCharacters.Contains(character) && !char.IsControl(character))]);
}
