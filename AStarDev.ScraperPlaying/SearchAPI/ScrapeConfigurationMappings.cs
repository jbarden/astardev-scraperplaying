using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.SearchAPI;

public static class ScrapeConfigurationMappings
{
    public static ScrapeConfiguration ToDto(this ScrapeConfigurationEntity config)
        => new(
            new Uri(config.SearchConfiguration.BaseUrl + config.SearchConfiguration.SearchStringPrefix),
            new Uri(config.SearchConfiguration.BaseUrl + config.SearchConfiguration.TopWallpapers),
            new Uri(config.SearchConfiguration.BaseUrl + config.SearchConfiguration.Subscriptions));
}