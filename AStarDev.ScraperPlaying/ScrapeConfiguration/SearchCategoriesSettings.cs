using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <summary>The validated search categories of a scrape configuration.</summary>
/// <param name="Categories">The complete list of categories the configuration should have after saving.</param>
public sealed record SearchCategoriesSettings(IReadOnlyList<SearchCategorySettings> Categories) : IScrapeConfigurationSectionEdit
{
    /// <inheritdoc/>
    /// <remarks>Categories are matched to existing ones by id, ignoring case. Matches keep their key and scrape progress, unmatched existing categories are removed and unmatched settings are added.</remarks>
    public void ApplyTo(ScrapeConfigurationEntity entity)
    {
        var search = entity.SearchConfiguration;
        var now = DateTimeOffset.UtcNow;

        foreach (var removed in search.SearchCategories.Where(existing => !Categories.Any(category => IsSameId(category.Id, existing.Id))).ToList())
        {
            _ = search.SearchCategories.Remove(removed);
        }

        foreach (var category in Categories)
        {
            var existing = search.SearchCategories.FirstOrDefault(candidate => IsSameId(category.Id, candidate.Id));
            if (existing is null)
            {
                search.SearchCategories.Add(new SearchCategoryEntity
                {
                    SearchConfigurationId = search.Id,
                    Id = category.Id,
                    Name = category.Name,
                    IncludeInSearch = category.IncludeInSearch,
                    IsFamous = category.IsFamous,
                    IsInternet = category.IsInternet
                });

                continue;
            }

            existing.Name = category.Name;
            existing.IncludeInSearch = category.IncludeInSearch;
            existing.IsFamous = category.IsFamous;
            existing.IsInternet = category.IsInternet;
            existing.UpdatedAt = now;
        }
    }

    private static bool IsSameId(string first, string second) => string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
}
