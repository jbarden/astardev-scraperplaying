using AStarDev.ScraperPlaying.Scraping;
using Shouldly;
using Xunit;

namespace AStarDev.ScraperPlaying.TestsIntegration.Scraping;

public sealed class GivenAPlaywrightBrowserSession
{
    [Fact]
    public async Task when_the_page_is_requested_then_a_working_headless_page_is_returned_and_reused()
    {
        await using var session = new PlaywrightBrowserSession();

        var firstPage = await session.GetPageAsync(useHeadless: true, CancellationToken.None);
        await firstPage.SetContentAsync("<html><body><h1>hello</h1></body></html>");
        var heading = await firstPage.TextContentAsync("h1");

        var secondPage = await session.GetPageAsync(useHeadless: true, CancellationToken.None);

        heading.ShouldBe("hello");
        secondPage.ShouldBeSameAs(firstPage);
    }
}
