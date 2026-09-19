using AStarDev.ScraperPlaying.Scraping;
using Microsoft.Playwright;

namespace AStarDev.ScraperPlaying.TestsUnit.Scraping;

public sealed class GivenASiteStatusChecker
{
    private static readonly WallhavenConnection Connection = new("api-key", new Uri("https://example.test/"), true);

    private readonly IPlaywrightBrowserSession browserSession = Substitute.For<IPlaywrightBrowserSession>();
    private readonly IPage page = Substitute.For<IPage>();
    private readonly List<string> navigatedTo = [];
    private readonly List<string> messages = [];
    private readonly SiteStatusChecker checker;

    public GivenASiteStatusChecker()
    {
        browserSession.GetPageAsync(true, Arg.Any<CancellationToken>()).Returns(page);
        page.GotoAsync(Arg.Any<string>(), Arg.Any<PageGotoOptions>()).Returns(call =>
        {
            navigatedTo.Add(call.Arg<string>());

            return Task.FromResult<IResponse?>(null);
        });
        page.TitleAsync().Returns("Wallhaven: Wallpapers");
        page.ContentAsync().Returns("<html><body>wallpapers</body></html>");
        checker = new(browserSession);
    }

    [Fact]
    public async Task when_the_site_shows_a_normal_page_then_it_is_available_and_nothing_is_reported()
    {
        var isAvailable = await Check();

        isAvailable.ShouldBeTrue();
        messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_the_site_is_checked_then_its_base_url_is_loaded()
    {
        await Check();

        navigatedTo.ShouldBe(["https://example.test/"]);
    }

    [Theory]
    [InlineData("wallhaven.cc Status")]
    [InlineData("WALLHAVEN.CC STATUS")]
    public async Task when_the_page_title_is_the_status_page_title_then_the_site_is_unavailable_and_it_is_reported(string title)
    {
        page.TitleAsync().Returns(title);

        var isAvailable = await Check();

        isAvailable.ShouldBeFalse();
        messages.ShouldHaveSingleItem().ShouldContain("example.test is showing its status page");
    }

    [Fact]
    public async Task when_the_page_says_the_site_is_taking_a_nap_then_the_site_is_unavailable()
    {
        page.ContentAsync().Returns("<html><body><h1>wallhaven.cc's taking a little nap</h1></body></html>");

        var isAvailable = await Check();

        isAvailable.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_browser_cannot_load_the_page_then_the_failure_propagates()
    {
        page.GotoAsync(Arg.Any<string>(), Arg.Any<PageGotoOptions>()).Returns<Task<IResponse?>>(_ => throw new PlaywrightException("net::ERR_CONNECTION_REFUSED"));

        await Should.ThrowAsync<PlaywrightException>(Check);
    }

    private Task<bool> Check() => checker.IsAvailableAsync(Connection, new Progress(messages), TestContext.Current.CancellationToken);

    private sealed class Progress(List<string> messages) : IProgress<string>
    {
        public void Report(string value) => messages.Add(value);
    }
}
