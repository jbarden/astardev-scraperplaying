using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using AStarDev.ScraperPlaying.Scraping;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using Shouldly;
using Xunit;

namespace AStarDev.ScraperPlaying.TestsIntegration.Scraping;

public sealed class GivenAPlaywrightBrowserSession
{
    [Fact]
    public async Task when_the_page_is_requested_then_a_working_page_is_returned_and_reused()
    {
        var userDataDirectory = CreateTemporaryUserDataDirectory();
        using var chromeProcess = StartChromeWithRemoteDebugging(userDataDirectory, out var cdpEndpointUrl);
        try
        {
            await WaitForCdpEndpointAsync(cdpEndpointUrl, TestContext.Current.CancellationToken);

            await using var session = new PlaywrightBrowserSession(BuildSettings(cdpEndpointUrl));

            var firstPage = await session.GetPageAsync(TestContext.Current.CancellationToken);
            await firstPage.SetContentAsync("<html><body><h1>hello</h1></body></html>");
            var heading = await firstPage.TextContentAsync("h1");

            var secondPage = await session.GetPageAsync(TestContext.Current.CancellationToken);

            heading.ShouldBe("hello");
            secondPage.ShouldBeSameAs(firstPage);
        }
        finally
        {
            StopChrome(chromeProcess);
            DeleteTemporaryUserDataDirectory(userDataDirectory);
        }
    }

    [Fact]
    public async Task when_the_session_is_disposed_then_the_attached_chrome_process_is_left_running()
    {
        var userDataDirectory = CreateTemporaryUserDataDirectory();
        using var chromeProcess = StartChromeWithRemoteDebugging(userDataDirectory, out var cdpEndpointUrl);
        try
        {
            await WaitForCdpEndpointAsync(cdpEndpointUrl, TestContext.Current.CancellationToken);

            await using (var session = new PlaywrightBrowserSession(BuildSettings(cdpEndpointUrl)))
            {
                await session.GetPageAsync(TestContext.Current.CancellationToken);
            }

            chromeProcess.HasExited.ShouldBeFalse();
        }
        finally
        {
            StopChrome(chromeProcess);
            DeleteTemporaryUserDataDirectory(userDataDirectory);
        }
    }

    [Fact]
    public async Task when_two_sessions_attach_to_the_same_running_chrome_then_browser_state_is_shared()
    {
        var userDataDirectory = CreateTemporaryUserDataDirectory();
        using var chromeProcess = StartChromeWithRemoteDebugging(userDataDirectory, out var cdpEndpointUrl);
        try
        {
            await WaitForCdpEndpointAsync(cdpEndpointUrl, TestContext.Current.CancellationToken);
            var settings = BuildSettings(cdpEndpointUrl);

            await using (var firstSession = new PlaywrightBrowserSession(settings))
            {
                var firstPage = await firstSession.GetPageAsync(TestContext.Current.CancellationToken);
                await firstPage.SetContentAsync("<html><body><h1>from the first session</h1></body></html>");
            }

            await using var secondSession = new PlaywrightBrowserSession(settings);
            var secondPage = await secondSession.GetPageAsync(TestContext.Current.CancellationToken);
            var heading = await secondPage.TextContentAsync("h1");

            heading.ShouldBe("from the first session");
        }
        finally
        {
            StopChrome(chromeProcess);
            DeleteTemporaryUserDataDirectory(userDataDirectory);
        }
    }

    private static string CreateTemporaryUserDataDirectory()
        => Path.Combine(Path.GetTempPath(), $"astardev-scraperplaying-tests-{Guid.NewGuid()}");

    private static void DeleteTemporaryUserDataDirectory(string userDataDirectory)
    {
        if (Directory.Exists(userDataDirectory)) Directory.Delete(userDataDirectory, recursive: true);
    }

    private static IOptions<ScraperAppSettings> BuildSettings(string cdpEndpointUrl)
        => Options.Create(new ScraperAppSettings { CdpEndpointUrl = cdpEndpointUrl });

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        return port;
    }

    private static Process StartChromeWithRemoteDebugging(string userDataDirectory, out string cdpEndpointUrl)
    {
        var port = GetFreeTcpPort();
        cdpEndpointUrl = $"http://localhost:{port}";

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "google-chrome",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                ArgumentList =
                {
                    $"--remote-debugging-port={port}",
                    $"--user-data-dir={userDataDirectory}",
                    "--headless=new",
                    "--no-first-run",
                    "about:blank",
                },
            },
        };
        process.Start();

        return process;
    }

    private static void StopChrome(Process chromeProcess)
    {
        if (!chromeProcess.HasExited) chromeProcess.Kill(entireProcessTree: true);
        chromeProcess.WaitForExit(TimeSpan.FromSeconds(5));
    }

    private static async Task WaitForCdpEndpointAsync(string cdpEndpointUrl, CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                var response = await httpClient.GetAsync($"{cdpEndpointUrl}/json/version", cancellationToken);
                if (response.IsSuccessStatusCode) return;
            }
            catch (HttpRequestException)
            {
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
        }

        throw new TimeoutException($"Chrome did not expose a CDP endpoint at {cdpEndpointUrl} in time.");
    }
}
