using AStarDev.ScraperPlaying.Scraping;
using Microsoft.Playwright;
using Shouldly;
using Xunit;

namespace AStarDev.ScraperPlaying.TestsIntegration.Scraping;

public sealed class GivenAWallpaperDetailPageScraper
{
    [Fact]
    public async Task when_scraping_a_page_with_wallhavens_real_markup_then_the_image_dimensions_and_tags_are_found()
    {
        var html = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Scraping", "WallpaperDetailPage.html"), TestContext.Current.CancellationToken);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await page.RouteAsync("https://example.test/w/pomle9", route => route.FulfillAsync(new RouteFulfillOptions { Body = html, ContentType = "text/html" }));

        var detail = await new WallpaperDetailPageScraper().ScrapeAsync("pomle9", page, new Uri("https://example.test"), new Progress<string>(), TestContext.Current.CancellationToken);

        detail.WallpaperId.ShouldBe("pomle9");
        detail.ImageUrl.ShouldBe("https://w.wallhaven.cc/full/po/wallhaven-pomle9.jpg");
        detail.DimensionX.ShouldBe(4096);
        detail.DimensionY.ShouldBe(2731);
        detail.Tags.ShouldBe([new WallpaperTag(7, "Japan"), new WallpaperTag(3359, "ginko"), new WallpaperTag(114467, "yellow leaves")]);
    }
}
