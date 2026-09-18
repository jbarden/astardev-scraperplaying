using Microsoft.Playwright;

namespace AStarDev.ScraperPlaying.Scraping;

/// <inheritdoc/>
public class WallpaperDetailPageScraper : IWallpaperDetailPageScraper
{
    private const string ScrapeScript = """
        () => {
            const image = document.getElementById('wallpaper');
            const resolutionText = document.querySelector('.showcase-resolution')?.textContent ?? '';
            const [widthText, heightText] = resolutionText.split('x');
            const tags = Array.from(document.querySelectorAll('#tags a.tag')).map(anchor => ({
                WallhavenTagId: parseInt((anchor.getAttribute('href') ?? '').split('/').pop(), 10),
                Name: anchor.textContent.trim()
            }));

            return {
                ImageUrl: image ? image.src : '',
                DimensionX: parseInt((widthText ?? '').trim(), 10) || 0,
                DimensionY: parseInt((heightText ?? '').trim(), 10) || 0,
                Tags: tags
            };
        }
        """;

    /// <inheritdoc/>
    public async Task<WallpaperDetail> ScrapeAsync(string wallpaperId, IPage page, Uri baseUrl, IProgress<string> progress, CancellationToken cancellationToken)
    {
        progress.Report($"Navigating to wallpaper {wallpaperId} detail page.");
        var targetUrl = new Uri(baseUrl, $"w/{wallpaperId}");

        await page.GotoAsync(targetUrl.ToString(), new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded }).WaitAsync(cancellationToken);

        var scraped = await page.EvaluateAsync<WallpaperDetailScrapeResult>(ScrapeScript).WaitAsync(cancellationToken);

        progress.Report($"Scraped wallpaper {wallpaperId}: {scraped.Tags.Count} tag(s).");

        return new WallpaperDetail(wallpaperId, scraped.ImageUrl, scraped.DimensionX, scraped.DimensionY, scraped.Tags);
    }
}
