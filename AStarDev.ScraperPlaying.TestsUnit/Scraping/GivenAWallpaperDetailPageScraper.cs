using AStarDev.ScraperPlaying.Scraping;
using Microsoft.Playwright;

namespace AStarDev.ScraperPlaying.TestsUnit.Scraping;

public sealed class GivenAWallpaperDetailPageScraper
{
    private readonly IPage page = Substitute.For<IPage>();
    private readonly CapturingProgress progress = new();
    private readonly WallpaperDetailPageScraper scraper = new();
    private string? navigatedUrl;

    public GivenAWallpaperDetailPageScraper()
    {
        page.GotoAsync(Arg.Any<string>(), Arg.Any<PageGotoOptions>())
            .Returns(callInfo =>
            {
                navigatedUrl = (string)callInfo[0];

                return Substitute.For<IResponse>();
            });
    }

    [Fact]
    public async Task when_a_detail_page_is_scraped_then_the_image_dimensions_and_tags_are_returned()
    {
        page.EvaluateAsync<WallpaperDetailScrapeResult>(Arg.Any<string>())
            .Returns(new WallpaperDetailScrapeResult(
                "https://w.wallhaven.cc/full/ab/wallhaven-abc123.jpg",
                1920,
                1080,
                [new WallpaperTag(1, "anime"), new WallpaperTag(2, "landscape")]));

        var result = await scraper.ScrapeAsync("abc123", page, new Uri("https://example.test"), progress, CancellationToken.None);

        result.WallpaperId.ShouldBe("abc123");
        result.ImageUrl.ShouldBe("https://w.wallhaven.cc/full/ab/wallhaven-abc123.jpg");
        result.DimensionX.ShouldBe(1920);
        result.DimensionY.ShouldBe(1080);
        result.Tags.ShouldBe([new WallpaperTag(1, "anime"), new WallpaperTag(2, "landscape")]);
    }

    [Fact]
    public async Task when_a_detail_page_is_scraped_then_its_own_url_is_navigated_to()
    {
        page.EvaluateAsync<WallpaperDetailScrapeResult>(Arg.Any<string>())
            .Returns(new WallpaperDetailScrapeResult(string.Empty, 0, 0, []));

        await scraper.ScrapeAsync("abc123", page, new Uri("https://example.test"), progress, CancellationToken.None);

        navigatedUrl.ShouldBe("https://example.test/w/abc123");
    }

    [Fact]
    public async Task when_a_detail_page_is_scraped_then_progress_is_reported()
    {
        page.EvaluateAsync<WallpaperDetailScrapeResult>(Arg.Any<string>())
            .Returns(new WallpaperDetailScrapeResult(string.Empty, 0, 0, [new WallpaperTag(1, "anime")]));

        await scraper.ScrapeAsync("abc123", page, new Uri("https://example.test"), progress, CancellationToken.None);

        progress.Messages.ShouldContain("Navigating to wallpaper abc123 detail page.");
        progress.Messages.ShouldContain("Scraped wallpaper abc123: 1 tag(s).");
    }

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }
}
