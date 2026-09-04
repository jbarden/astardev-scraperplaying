
using AStarDev.ControlDb;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ScraperPlaying.SearchAPI;

/// <summary>
/// Represents a repository for scrape configuration settings repository.
/// </summary>
/// <param name="dbContextFactory">The factory for creating instances of the ControlDbContext.</param>
public sealed class ScrapeConfigurationRepository(IDbContextFactory<ControlDbContext> dbContextFactory) : IScrapeConfigurationRepository
{
    /// <inheritdoc/>
    public async Task<Exceptional<ScrapeConfiguration>> GetScrapeConfigurationAsync()
        => await Try.RunAsync(async () =>
            {
                using var dbContext = dbContextFactory.CreateDbContext();
                
                return (await dbContext.ScrapeConfigurations.Include(sc => sc.ScrapeDirectories).Include(sc => sc.UserConfiguration)
                .Include(sc => sc.SearchConfiguration).FirstAsync()).ToDto(); 
            });
}
