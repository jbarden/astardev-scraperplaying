using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;
using Microsoft.Playwright;

namespace AStarDev.ScraperPlaying.TestsUnit.Scraping;

public sealed class GivenAWebsitePagesProcessor
{
    private readonly IPlaywrightBrowserSession browserSession = Substitute.For<IPlaywrightBrowserSession>();
    private readonly IPage page = Substitute.For<IPage>();
    private readonly ILocator locator = Substitute.For<ILocator>();
    private readonly Dictionary<int, string[]> wallpapersByPage = [];
    private readonly CapturingProgress progress = new();
    private readonly WebsitePagesProcessor processor;

    private int currentPageNumber;

    public GivenAWebsitePagesProcessor()
    {
        browserSession.GetPageAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(page);
        page.GotoAsync(Arg.Any<string>(), Arg.Any<PageGotoOptions>())
            .Returns(callInfo =>
            {
                currentPageNumber = int.Parse(((string)callInfo[0]).Split('/')[^1]);

                return Substitute.For<IResponse>();
            });
        page.Locator("figure.thumb", Arg.Any<PageLocatorOptions?>()).Returns(locator);
        locator.EvaluateAllAsync<string[]>(Arg.Any<string>(), Arg.Any<object?>())
            .Returns(_ => wallpapersByPage.GetValueOrDefault(currentPageNumber, []));
        processor = new(browserSession, () => TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task when_a_page_has_wallpapers_then_they_are_reported_and_the_next_page_is_fetched()
    {
        wallpapersByPage[1] = ["wallpaper-1", "wallpaper-2"];

        await Run();

        progress.Messages.ShouldContain("Navigating to wallpapers page 1.");
        progress.Messages.ShouldContain("Found 2 wallpaper(s) on wallpapers page 1: wallpaper-1, wallpaper-2.");
        progress.Messages.ShouldContain("Navigating to wallpapers page 2.");
        progress.Messages.ShouldContain("No wallpapers found on wallpapers page 2.");
        progress.Messages.ShouldNotContain(message => message.Contains("page 3"));
    }

    [Fact]
    public async Task when_the_hard_cap_is_reached_then_paging_stops_at_page_4()
    {
        wallpapersByPage[1] = ["wallpaper-1"];
        wallpapersByPage[2] = ["wallpaper-2"];
        wallpapersByPage[3] = ["wallpaper-3"];
        wallpapersByPage[4] = ["wallpaper-4"];
        wallpapersByPage[5] = ["wallpaper-5"];

        await Run();

        progress.Messages.ShouldContain("Navigating to wallpapers page 4.");
        progress.Messages.ShouldNotContain(message => message.Contains("page 5"));
    }

    [Fact]
    public async Task when_navigation_fails_then_the_failure_is_reported_and_rethrown()
    {
        var exception = new PlaywrightException("navigation failed");
        page.GotoAsync(Arg.Any<string>(), Arg.Any<PageGotoOptions>()).Returns<IResponse?>(_ => throw exception);

        var thrown = await Should.ThrowAsync<PlaywrightException>(Run);

        thrown.ShouldBeSameAs(exception);
        progress.Messages.ShouldContain("An error occurred navigating the website during wallpapers: navigation failed");
    }

    [Fact]
    public async Task when_cancelled_then_cancellation_is_reported_and_rethrown()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(
            () => processor.FetchAndProcessPagesAsync("wallpapers", Option.None<string>(), page => $"page/{page}", new WallhavenConnection("api-key", new Uri("https://example.test")), progress, cancellationTokenSource.Token));

        progress.Messages.ShouldContain("Scrape cancelled.");
    }

    private Task Run()
        => processor.FetchAndProcessPagesAsync("wallpapers", Option.None<string>(), page => $"page/{page}", new WallhavenConnection("api-key", new Uri("https://example.test")), progress, CancellationToken.None);

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }
}
