using AStarDev.ScraperPlaying.Scraping;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using Shouldly;
using Xunit;

namespace AStarDev.ScraperPlaying.TestsIntegration.Scraping;

public sealed class GivenAPlaywrightBrowserSession
{
    [Fact]
    public async Task when_the_page_is_requested_then_a_working_headless_page_is_returned_and_reused()
    {
        var userDataDirectory = CreateTemporaryUserDataDirectory();
        try
        {
            await using var session = new PlaywrightBrowserSession(BuildSettings(userDataDirectory));

            var firstPage = await session.GetPageAsync(useHeadless: true, CancellationToken.None);
            await firstPage.SetContentAsync("<html><body><h1>hello</h1></body></html>");
            var heading = await firstPage.TextContentAsync("h1");

            var secondPage = await session.GetPageAsync(useHeadless: true, CancellationToken.None);

            heading.ShouldBe("hello");
            secondPage.ShouldBeSameAs(firstPage);
        }
        finally
        {
            DeleteTemporaryUserDataDirectory(userDataDirectory);
        }
    }

    [Fact]
    public async Task when_two_sessions_share_a_user_data_directory_then_browser_state_persists_between_them()
    {
        var userDataDirectory = CreateTemporaryUserDataDirectory();
        try
        {
            var settings = BuildSettings(userDataDirectory);

            await using (var firstSession = new PlaywrightBrowserSession(settings))
            {
                var firstPage = await firstSession.GetPageAsync(useHeadless: true, CancellationToken.None);
                await firstPage.Context.AddCookiesAsync([
                    new Cookie
                    {
                        Name = "loginToken",
                        Value = "abc123",
                        Domain = "wallhaven.cc",
                        Path = "/",
                        Expires = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds(),
                    },
                ]);
            }

            await using var secondSession = new PlaywrightBrowserSession(settings);
            var secondPage = await secondSession.GetPageAsync(useHeadless: true, CancellationToken.None);
            var persistedCookies = await secondPage.Context.CookiesAsync(["https://wallhaven.cc"]);

            persistedCookies.ShouldContain(cookie => cookie.Name == "loginToken" && cookie.Value == "abc123");
        }
        finally
        {
            DeleteTemporaryUserDataDirectory(userDataDirectory);
        }
    }

    private static string CreateTemporaryUserDataDirectory()
        => Path.Combine(Path.GetTempPath(), $"astardev-scraperplaying-tests-{Guid.NewGuid()}");

    private static void DeleteTemporaryUserDataDirectory(string userDataDirectory)
    {
        if (Directory.Exists(userDataDirectory)) Directory.Delete(userDataDirectory, recursive: true);
    }

    private static IOptions<ScraperAppSettings> BuildSettings(string userDataDirectory)
        => Options.Create(new ScraperAppSettings { UserDataDirectory = userDataDirectory });
}
