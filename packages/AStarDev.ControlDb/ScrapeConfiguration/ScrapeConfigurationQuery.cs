using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb.ScrapeConfiguration;

/// <summary>
/// Eagerly loads a <see cref="ScrapeConfigurationEntity"/> aggregate together with its required sub-entities
/// (<see cref="ScrapeConfigurationEntity.UserConfiguration"/>, <see cref="ScrapeConfigurationEntity.SearchConfiguration"/>,
/// its <see cref="SearchConfigurationEntity.SearchCategories"/>, and <see cref="ScrapeConfigurationEntity.ScrapeDirectories"/>).
/// </summary>
public sealed class ScrapeConfigurationQuery : IQuery<ScrapeConfigurationEntity>
{
    /// <inheritdoc/>
    public IQueryable<ScrapeConfigurationEntity> Apply(IQueryable<ScrapeConfigurationEntity> query) =>
        query
            .Include(scrapeConfiguration => scrapeConfiguration.UserConfiguration)
            .Include(scrapeConfiguration => scrapeConfiguration.SearchConfiguration)
                .ThenInclude(searchConfiguration => searchConfiguration.SearchCategories)
            .Include(scrapeConfiguration => scrapeConfiguration.ScrapeDirectories);
}
