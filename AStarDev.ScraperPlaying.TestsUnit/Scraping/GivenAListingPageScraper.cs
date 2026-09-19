using AStarDev.ScraperPlaying.Scraping;
using Microsoft.Playwright;

namespace AStarDev.ScraperPlaying.TestsUnit.Scraping;

public sealed class GivenAListingPageScraper
{
    private readonly IPage page = Substitute.For<IPage>();
    private readonly ILocator locator = Substitute.For<ILocator>();
    private readonly CapturingProgress progress = new();
    private readonly ListingPageScraper scraper;

    private int pacingDelayCount;

    public GivenAListingPageScraper()
    {
        page.GotoAsync(Arg.Any<string>(), Arg.Any<PageGotoOptions>()).Returns(Substitute.For<IResponse>());
        page.Locator("figure.thumb", Arg.Any<PageLocatorOptions?>()).Returns(locator);
        locator.EvaluateAllAsync<string[]>(Arg.Any<string>(), Arg.Any<object?>()).Returns(["wallpaper-1", "wallpaper-2"]);
        scraper = new(() =>
        {
            pacingDelayCount++;

            return TimeSpan.FromMilliseconds(1);
        });
    }

    [Fact]
    public async Task when_the_page_has_wallpapers_then_their_ids_are_returned_and_reported()
    {
        var ids = await Run();

        ids.ShouldBe(["wallpaper-1", "wallpaper-2"]);
        progress.Messages.ShouldContain("Navigating to wallpapers page 3.");
        progress.Messages.ShouldContain("Found 2 wallpaper(s) on wallpapers page 3: wallpaper-1, wallpaper-2.");
    }

    [Fact]
    public async Task when_the_page_is_navigated_to_then_the_request_url_is_used()
    {
        await Run();

        await page.Received(1).GotoAsync("https://example.test/page/3", Arg.Any<PageGotoOptions>());
    }

    [Fact]
    public async Task when_the_page_has_no_wallpapers_then_an_empty_array_is_returned_and_reported()
    {
        locator.EvaluateAllAsync<string[]>(Arg.Any<string>(), Arg.Any<object?>()).Returns([]);

        var ids = await Run();

        ids.ShouldBeEmpty();
        progress.Messages.ShouldContain("No wallpapers found on wallpapers page 3.");
    }

    [Fact]
    public async Task when_scraping_a_page_then_a_single_pacing_delay_is_applied()
    {
        await Run();

        pacingDelayCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_navigation_fails_then_the_failure_propagates()
    {
        var exception = new PlaywrightException("navigation failed");
        page.GotoAsync(Arg.Any<string>(), Arg.Any<PageGotoOptions>()).Returns<IResponse?>(_ => throw exception);

        var thrown = await Should.ThrowAsync<PlaywrightException>(Run);

        thrown.ShouldBeSameAs(exception);
    }

    private Task<string[]> Run()
        => scraper.ScrapeWallpaperIdsAsync(new ListingPageRequest("wallpapers", 3, new Uri("https://example.test/page/3")), page, progress, CancellationToken.None);

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }
}
