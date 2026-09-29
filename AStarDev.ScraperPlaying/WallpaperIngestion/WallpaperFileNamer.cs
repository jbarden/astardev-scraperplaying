using AStarDev.ControlDb.FileDetail;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.DetailResponse;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <summary>Generates the file name a wallpaper is saved and recorded under.</summary>
public static class WallpaperFileNamer
{
    private const int MaxPrefixLength = 100;
    private const string InvalidFileNameCharacters = "\\/:*?\"<>|";

    /// <summary>Generates the file name for a wallpaper. Wallpapers with person-name tags (a tag in a person category whose name starts with an upper-case letter, which rules out descriptive tags such as "finger pointing") are prefixed with those names, joined by underscores; all others use just the wallpaper id.</summary>
    /// <param name="wallpaperId">Wallhaven's id for the wallpaper.</param>
    /// <param name="extension">The file extension, including the leading '.'.</param>
    /// <param name="tags">The wallpaper's tags.</param>
    /// <param name="personCategories">The tag category names whose tags are person names, matched case-insensitively.</param>
    /// <returns>The file name.</returns>
    public static FileName Create(string wallpaperId, string extension, IReadOnlyList<Tag> tags, IReadOnlyList<string> personCategories)
    {
        var prefix = CreatePrefix(tags, personCategories);
        var baseName = prefix.Length == 0 ? wallpaperId : $"{prefix}_{wallpaperId}";

        return new FileName($"{baseName}{extension}");
    }

    private static string CreatePrefix(IReadOnlyList<Tag> tags, IReadOnlyList<string> personCategories)
    {
        var joined = string.Join('_', tags.Where(tag => IsPersonName(tag, personCategories)).Select(tag => Sanitise(tag.Name)).Where(name => name.Length > 0));

        return joined.Length > MaxPrefixLength ? joined[..MaxPrefixLength] : joined;
    }

    private static bool IsPersonName(Tag tag, IReadOnlyList<string> personCategories)
        => tag.Name.Length > 0 && char.IsUpper(tag.Name[0]) && personCategories.Contains(tag.Category, StringComparer.OrdinalIgnoreCase);

    private static string Sanitise(string tagName)
        => new([.. tagName.Trim().Replace(' ', '_').Where(character => !InvalidFileNameCharacters.Contains(character) && !char.IsControl(character))]);
}
