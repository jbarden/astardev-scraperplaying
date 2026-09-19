using Microsoft.Playwright;

namespace AStarDev.ScraperPlaying.Scraping;

/// <inheritdoc/>
public class SiteStatusChecker(IPlaywrightBrowserSession browserSession) : ISiteStatusChecker
{
    private const string StatusPageTitle = "wallhaven.cc Status";
    private const string StatusPageText = "taking a little nap";

    /// <inheritdoc/>
    public async Task<bool> IsAvailableAsync(WallhavenConnection connection, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var page = await browserSession.GetPageAsync(connection.UseHeadless, cancellationToken);
        await page.GotoAsync(connection.BaseUrl.ToString(), new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded }).WaitAsync(cancellationToken);

        var title = await page.TitleAsync().WaitAsync(cancellationToken);
        var content = await page.ContentAsync().WaitAsync(cancellationToken);
        var isStatusPage = title.Contains(StatusPageTitle, StringComparison.OrdinalIgnoreCase) || content.Contains(StatusPageText, StringComparison.OrdinalIgnoreCase);
        if (isStatusPage) progress.Report($"{connection.BaseUrl.Host} is showing its status page - it looks to be down or in maintenance.");

        return !isStatusPage;
    }
}
