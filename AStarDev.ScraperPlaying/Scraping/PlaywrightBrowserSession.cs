using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>
/// Attaches, via Chrome DevTools Protocol, to an already-running Chrome instance for the lifetime of
/// the owning DI scope (one scrape run), and reuses its default context and page. The browser is
/// launched normally by the user (not by this class) at <see cref="ScraperAppSettings.CdpEndpointUrl" />,
/// so it carries none of the automation flags a Playwright-launched browser would, and can be logged
/// in and past any bot-detection challenge before a scrape starts. Reusing a single page across a
/// scrape that can run for hours avoids the overhead and resource churn of repeatedly resolving a
/// page per page/category. Disposal only disconnects the Playwright driver - it never closes the
/// browser, context, or page, since those belong to the user's own already-running browser.
/// </summary>
/// <param name="settings">The scraper app settings, providing the CDP endpoint to attach to.</param>
/// <inheritdoc/>
public sealed class PlaywrightBrowserSession(IOptions<ScraperAppSettings> settings) : IPlaywrightBrowserSession, IAsyncDisposable
{
    private readonly SemaphoreSlim initializationLock = new(1, 1);
    private IPlaywright? playwright;
    private IPage? page;

    /// <inheritdoc/>
    public async Task<IPage> GetPageAsync(CancellationToken cancellationToken)
    {
        if (page is not null) return page;

        await initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (page is not null) return page;

            playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.ConnectOverCDPAsync(settings.Value.CdpEndpointUrl);
            var context = browser.Contexts.Count > 0 ? browser.Contexts[0] : await browser.NewContextAsync();
            context.SetDefaultTimeout(30_000);
            page = context.Pages.Count > 0 ? context.Pages[0] : await context.NewPageAsync();

            return page;
        }
        finally
        {
            initializationLock.Release();
        }
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        playwright?.Dispose();
        initializationLock.Dispose();

        return ValueTask.CompletedTask;
    }
}
