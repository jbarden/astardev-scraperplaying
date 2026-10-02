using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Interface for processing pages by fetching and handling their content asynchronously.</summary>
public interface IPagesProcessor
{
    /// <summary>Fetches and processes pages asynchronously based on the provided request.</summary>
    /// <param name="request">Describes the scrape: label, category, previous progress, page URLs, connection and person categories.</param>
    /// <param name="progress">The progress reporter to report the fetching and processing progress.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The progress of the last page fully ingested by this scrape, or <see cref="Option{T}.None"/> when the search was skipped because nothing had changed since the previous scrape or no page was fully ingested (a page with a failed wallpaper holds progress back, so it is retried).</returns>
    Task<Option<SearchCategoryProgress>> FetchAndProcessPagesAsync(PageScrapeRequest request, IProgress<string> progress, CancellationToken cancellationToken);
}
