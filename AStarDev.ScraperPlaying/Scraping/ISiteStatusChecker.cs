namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Interface for checking that the website to scrape is up, rather than showing its status or maintenance page.</summary>
public interface ISiteStatusChecker
{
    /// <summary>Loads the website's base URL in the scrape's browser page and checks it is not showing its status page.</summary>
    /// <param name="connection">The target host to check.</param>
    /// <param name="progress">The progress reporter to report why the site is unavailable.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task containing <see langword="true"/> when the site is available, or <see langword="false"/> when it is showing its status page.</returns>
    Task<bool> IsAvailableAsync(WallhavenConnection connection, IProgress<string> progress, CancellationToken cancellationToken);
}
