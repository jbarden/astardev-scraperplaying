using System.Diagnostics.CodeAnalysis;
using AStarDev.ScraperPlaying.Scraping;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>Starts a scrape from the main window, showing its progress in the status and reporting any failure instead of letting it escape the button handler.</summary>
/// <param name="scrapeService">The service that performs the scrape.</param>
/// <param name="status">Where progress and failures are reported.</param>
public sealed class ScrapeRunner(IScrapeService scrapeService, StatusReporter status)
{
    /// <summary>Runs the scraper.</summary>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Called from an async void UI handler: any exception escaping it would crash the application, so every failure is reported in the status instead.")]
    public async Task RunAsync()
    {
        try
        {
            await scrapeService.RunScraperAsync(new StatusProgress(status));
        }
        catch (Exception exception)
        {
            status.Error("The scrape failed.", exception);
        }
    }

    private sealed class StatusProgress(StatusReporter status) : IProgress<string>
    {
        public void Report(string value) => status.Append(value);
    }
}
