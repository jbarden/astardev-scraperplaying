using AStarDev.Utilities;
using Microsoft.Playwright;

namespace AStarDev.ScraperPlaying.Scraping;

/// <inheritdoc/>
public class WallpaperDetailPageScraper : IWallpaperDetailPageScraper
{
    private const string ScrapeScript = """
        () => {
            const image = document.getElementById('wallpaper');
            const dimension = name => parseInt(image?.dataset[name] ?? '', 10) || 0;
            const tags = Array.from(document.querySelectorAll('#tags li.tag'))
                .map(item => ({
                    wallhavenTagId: parseInt(item.dataset.tagId ?? '', 10),
                    name: (item.querySelector('a.tagname')?.textContent ?? '').trim()
                }))
                .filter(tag => tag.wallhavenTagId && tag.name);

            return JSON.stringify({
                imageUrl: image ? image.src : '',
                dimensionX: dimension('wallpaperWidth'),
                dimensionY: dimension('wallpaperHeight'),
                tags: tags
            });
        }
        """;

    /// <inheritdoc/>
    public async Task<WallpaperDetail> ScrapeAsync(string wallpaperId, IPage page, Uri baseUrl, IProgress<string> progress, CancellationToken cancellationToken)
    {
        progress.Report($"Navigating to wallpaper {wallpaperId} detail page.");
        var targetUrl = new Uri(baseUrl, $"w/{wallpaperId}");

        await page.GotoAsync(targetUrl.ToString(), new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded }).WaitAsync(cancellationToken);

        var scraped = (await page.EvaluateAsync<string>(ScrapeScript).WaitAsync(cancellationToken)).FromJson<WallpaperDetailScrapeResult>();

        progress.Report($"Scraped wallpaper {wallpaperId}: {scraped.Tags.Count} tag(s).");

        return new WallpaperDetail(wallpaperId, scraped.ImageUrl, scraped.DimensionX, scraped.DimensionY, scraped.Tags);
    }
}
