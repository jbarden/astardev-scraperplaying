using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <summary>The validated root-level settings of a scrape configuration.</summary>
public sealed record RootSettings(
    Uri BaseUrl,
    Uri LoginUrl,
    string ApiKey,
    string SearchString,
    string TopWallpapers,
    string SearchStringPrefix,
    string SearchStringSuffix,
    string Subscriptions,
    int ImagePauseInSeconds,
    int StartingPageNumber,
    int TotalPages,
    int SubscriptionsStartingPageNumber,
    int SubscriptionsTotalPages,
    int TopWallpapersStartingPageNumber,
    int TopWallpapersTotalPages,
    bool UseHeadless,
    Option<float> SlowMotionDelay) : IScrapeConfigurationSectionEdit
{
    /// <summary>Copies the root-level settings from the specified scrape configuration.</summary>
    /// <param name="entity">The scrape configuration to read.</param>
    public static RootSettings From(ScrapeConfigurationEntity entity) => new(
        entity.BaseUrl,
        entity.LoginUrl,
        entity.ApiKey,
        entity.SearchString,
        entity.TopWallpapers,
        entity.SearchStringPrefix,
        entity.SearchStringSuffix,
        entity.Subscriptions,
        entity.ImagePauseInSeconds,
        entity.StartingPageNumber,
        entity.TotalPages,
        entity.SubscriptionsStartingPageNumber,
        entity.SubscriptionsTotalPages,
        entity.TopWallpapersStartingPageNumber,
        entity.TopWallpapersTotalPages,
        entity.UseHeadless,
        entity.SlowMotionDelay is { } delay ? Option.Some(delay) : Option.None<float>());

    /// <inheritdoc/>
    public void ApplyTo(ScrapeConfigurationEntity entity)
    {
        entity.BaseUrl = BaseUrl;
        entity.LoginUrl = LoginUrl;
        entity.ApiKey = ApiKey;
        entity.SearchString = SearchString;
        entity.TopWallpapers = TopWallpapers;
        entity.SearchStringPrefix = SearchStringPrefix;
        entity.SearchStringSuffix = SearchStringSuffix;
        entity.Subscriptions = Subscriptions;
        entity.ImagePauseInSeconds = ImagePauseInSeconds;
        entity.StartingPageNumber = StartingPageNumber;
        entity.TotalPages = TotalPages;
        entity.SubscriptionsStartingPageNumber = SubscriptionsStartingPageNumber;
        entity.SubscriptionsTotalPages = SubscriptionsTotalPages;
        entity.TopWallpapersStartingPageNumber = TopWallpapersStartingPageNumber;
        entity.TopWallpapersTotalPages = TopWallpapersTotalPages;
        entity.UseHeadless = UseHeadless;
        entity.SlowMotionDelay = SlowMotionDelay.Match(delay => (float?)delay, () => null);
    }
}
