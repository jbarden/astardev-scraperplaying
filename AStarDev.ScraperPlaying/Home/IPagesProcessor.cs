namespace AStarDev.ScraperPlaying.Home;

/// <summary>
/// Interface for processing pages by fetching and handling their content asynchronously.
/// </summary>
public interface IPagesProcessor
{
    /// <summary>
    /// Fetches and processes pages asynchronously based on the provided parameters.
    /// </summary>
    /// <param name="logLabel">A label used for logging purposes.</param>
    /// <param name="pageUrlFactory">A function that generates page URLs based on the page number.</param>
    /// <param name="sessionCookie">The session cookie used for authentication.</param>
    /// <param name="baseUrl">The base URL of the website to fetch pages from.</param>
    /// <param name="progress">The progress reporter to report the fetching and processing progress.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task FetchAndProcessPagesAsync(string logLabel, Func<int, string> pageUrlFactory, string sessionCookie, Uri baseUrl, IProgress<string> progress, CancellationToken cancellationToken);
}