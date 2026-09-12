using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.SearchAPI;

public static class ScrapeConfigurationMappings
{
    public static ScrapeConfiguration ToDto(this ScrapeConfigurationEntity config)
        => new(
            new Uri(config.BaseUrl.AbsoluteUri + config.SearchStringPrefix + "{id}" + config.SearchStringSuffix),
            new Uri(config.BaseUrl.AbsoluteUri + config.TopWallpapers),
            new Uri(config.BaseUrl.AbsoluteUri + config.Subscriptions),
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
