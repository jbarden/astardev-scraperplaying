using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Interface for processing pages by fetching and handling their content asynchronously.</summary>
public interface IPagesProcessor
{
    /// <summary>Fetches and processes pages asynchronously based on the provided parameters.</summary>
    /// <param name="request">The search to fetch and process: its label, category and page URL factory.</param>
    /// <param name="connection">The Wallhaven API credentials and target host to fetch pages from.</param>
    /// <param name="progress">The progress reporter to report the fetching and processing progress.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task FetchAndProcessPagesAsync(SearchRequest request, WallhavenConnection connection, IProgress<string> progress, CancellationToken cancellationToken);
}