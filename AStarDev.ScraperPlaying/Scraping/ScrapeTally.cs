using AStarDev.ScraperPlaying.WallpaperIngestion;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Counts the wallpapers one search has downloaded against the total the search matches.</summary>
public sealed class ScrapeTally
{
    private int current;

    /// <summary>Gets or sets the total number of wallpapers the search matches, as reported by the latest page fetched.</summary>
    public int Total { get; set; }

    /// <summary>Counts one more downloaded wallpaper.</summary>
    /// <returns>The count including the wallpaper just downloaded.</returns>
    public SearchCount RecordDownload() => new(++current, Total);
}
