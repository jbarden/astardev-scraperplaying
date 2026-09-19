using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <summary>Loads the stored scrape directories.</summary>
public static class ScrapeDirectoriesLoader
{
    /// <summary>Loads the stored scrape directories.</summary>
    /// <param name="query">The query used to read the directories.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The scrape directories.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no scrape configuration is stored.</exception>
    public static async Task<ScrapeDirectoriesEntity> LoadAsync(IScrapeDirectoriesQuery query, CancellationToken cancellationToken = default)
        => (await query.GetAsync(cancellationToken))
            .Match(
                option => option.Match(directories => directories, () => throw new InvalidOperationException("Scrape configuration not found")),
                exception => throw exception);
}
