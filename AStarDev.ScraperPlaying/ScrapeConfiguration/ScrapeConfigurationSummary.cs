using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <summary>Identifies a root scrape configuration and the label shown for it in the configuration picker.</summary>
/// <param name="Id">The unique identifier of the scrape configuration.</param>
/// <param name="Label">The human-readable label describing the scrape configuration.</param>
public sealed record ScrapeConfigurationSummary(ScrapeConfigurationId Id, string Label)
{
    /// <summary>Creates a summary for the specified scrape configuration.</summary>
    /// <param name="entity">The scrape configuration to summarise.</param>
    /// <returns>The summary, labelled with the site host and, when set, the search term.</returns>
    public static ScrapeConfigurationSummary From(ScrapeConfigurationEntity entity)
    {
        var site = entity.BaseUrl.IsAbsoluteUri ? entity.BaseUrl.Host : entity.BaseUrl.OriginalString;
        var searchTerm = entity.SearchConfiguration.SearchTerm;

        return new ScrapeConfigurationSummary(entity.Id, string.IsNullOrWhiteSpace(searchTerm) ? site : $"{site} - {searchTerm}");
    }

    /// <inheritdoc/>
    public override string ToString() => Label;
}
