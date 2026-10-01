using System.Diagnostics.CodeAnalysis;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <summary>The validated root-level settings of a scrape configuration.</summary>
[SuppressMessage("Design", "CA1054:URI parameters should not be strings", Justification = "The login page is free text and is not required to be a valid URL.")]
[SuppressMessage("Design", "CA1056:URI properties should not be strings", Justification = "The login page is free text and is not required to be a valid URL.")]
public sealed record RootSettings(
    Uri BaseUrl,
    string LoginUrl,
    string ApiKey,
    string SearchString,
    string TopWallpapers,
    string HotWallpapers,
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
    int HotWallpapersStartingPageNumber,
    int HotWallpapersTotalPages,
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
        entity.HotWallpapers,
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
        entity.HotWallpapersStartingPageNumber,
        entity.HotWallpapersTotalPages,
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
        entity.HotWallpapers = HotWallpapers;
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
        entity.HotWallpapersStartingPageNumber = HotWallpapersStartingPageNumber;
        entity.HotWallpapersTotalPages = HotWallpapersTotalPages;
        entity.UseHeadless = UseHeadless;
        entity.SlowMotionDelay = SlowMotionDelay.Match(delay => (float?)delay, () => null);
    }
}
