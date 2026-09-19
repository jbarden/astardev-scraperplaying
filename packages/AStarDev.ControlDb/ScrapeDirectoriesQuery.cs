using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb;

/// <summary>Implementation of <see cref="IScrapeDirectoriesQuery"/>: one small untracked query on the directories table.</summary>
/// <param name="context">The database context used for querying.</param>
public class ScrapeDirectoriesQuery(ControlDbContext context) : IScrapeDirectoriesQuery
{
    /// <inheritdoc/>
    public Task<Exceptional<Option<ScrapeDirectoriesEntity>>> GetAsync(CancellationToken cancellationToken = default)
        => Try.RunAsync(async () => (Option<ScrapeDirectoriesEntity>)await context.Set<ScrapeDirectoriesEntity>().AsNoTracking().FirstOrDefaultAsync(cancellationToken));
}
