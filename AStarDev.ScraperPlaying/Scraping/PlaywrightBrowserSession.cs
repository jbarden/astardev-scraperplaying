using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>
/// Manages a single Chromium persistent-context browser and page for the lifetime of the owning DI
/// scope (one scrape run). The context is launched against <see cref="ScraperAppSettings.UserDataDirectory" />
/// so cookies and login state persist between runs, using the real installed Google Chrome binary
/// (rather than Playwright's bundled Chromium build) since Cloudflare's bot detection fingerprints the
/// bundled build specifically. Reusing a single page across a scrape that can run for hours avoids the
/// overhead and resource churn of repeatedly launching a browser per page/category.
/// </summary>
/// <param name="settings">The scraper app settings, providing the persistent-context user data directory.</param>
/// <inheritdoc/>
public sealed class PlaywrightBrowserSession(IOptions<ScraperAppSettings> settings) : IPlaywrightBrowserSession, IAsyncDisposable
{
    private readonly SemaphoreSlim initializationLock = new(1, 1);
    private IPlaywright? playwright;
    private IBrowserContext? context;
    private IPage? page;

    /// <inheritdoc/>
    public async Task<IPage> GetPageAsync(bool useHeadless, CancellationToken cancellationToken)
    {
        if (page is not null) return page;

        await initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (page is not null) return page;

            playwright = await Playwright.CreateAsync();
            context = await playwright.Chromium.LaunchPersistentContextAsync(settings.Value.UserDataDirectory, new BrowserTypeLaunchPersistentContextOptions
            {
                Headless = useHeadless,
                Channel = "chrome",
            });
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
    public async ValueTask DisposeAsync()
    {
        if (context is not null) await context.CloseAsync();
        playwright?.Dispose();
        initializationLock.Dispose();
    }
}
