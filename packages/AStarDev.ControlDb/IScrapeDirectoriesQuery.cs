using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ControlDb;

/// <summary>Interface for reading just the scrape directories, without loading the rest of the scrape configuration.</summary>
public interface IScrapeDirectoriesQuery
{
    /// <summary>Gets the stored scrape directories. The returned entity is not tracked by the database context.</summary>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    /// <returns>An exceptional result containing the directories, or an empty option when no scrape configuration is stored.</returns>
    Task<Exceptional<Option<ScrapeDirectoriesEntity>>> GetAsync(CancellationToken cancellationToken = default);
}
