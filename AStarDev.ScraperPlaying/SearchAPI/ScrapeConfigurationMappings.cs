using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.SearchAPI;

public static class ScrapeConfigurationMappings
{
    public static ScrapeConfiguration ToDto(this ScrapeConfigurationEntity config)
        => new(
            new Uri(config.SearchConfiguration.BaseUrl.AbsoluteUri + config.SearchConfiguration.SearchStringPrefix + "{id}" + config.SearchConfiguration.SearchStringSuffix),
            new Uri(config.SearchConfiguration.BaseUrl.AbsoluteUri + config.SearchConfiguration.TopWallpapers),
            new Uri(config.SearchConfiguration.BaseUrl.AbsoluteUri + config.SearchConfiguration.Subscriptions),
            config.SearchConfiguration.SearchCategories.ToDtos(), config.UserConfiguration.ToDto());

    public static UserConfiguration ToDto(this UserConfigurationEntity config)
        => new(
            config.Id.ToString(),
            config.ScrapeConfigurationEntityId.ToString(),
            config.EmailAddress,
            config.Username,
            config.Password,
            config.ApiKey
        );
}
