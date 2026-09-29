using System.Diagnostics.CodeAnalysis;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <summary>The unvalidated root-level settings as entered in the configuration editor.</summary>
[SuppressMessage("Design", "CA1054:URI parameters should not be strings", Justification = "Holds raw text typed into the editor; parsed and validated by Validate.")]
[SuppressMessage("Design", "CA1056:URI properties should not be strings", Justification = "Holds raw text typed into the editor; parsed and validated by Validate.")]
public sealed record RootSettingsInput(
    string BaseUrl,
    string LoginUrl,
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
    Option<float> SlowMotionDelay)
{
    /// <summary>Creates the editor input from the specified scrape configuration.</summary>
    /// <param name="entity">The scrape configuration to read.</param>
    public static RootSettingsInput From(ScrapeConfigurationEntity entity)
    {
        var settings = RootSettings.From(entity);

        return new RootSettingsInput(
            settings.BaseUrl.ToString(),
            settings.LoginUrl,
            settings.ApiKey,
            settings.SearchString,
            settings.TopWallpapers,
            settings.SearchStringPrefix,
            settings.SearchStringSuffix,
            settings.Subscriptions,
            settings.ImagePauseInSeconds,
            settings.StartingPageNumber,
            settings.TotalPages,
            settings.SubscriptionsStartingPageNumber,
            settings.SubscriptionsTotalPages,
            settings.TopWallpapersStartingPageNumber,
            settings.TopWallpapersTotalPages,
            settings.UseHeadless,
            settings.SlowMotionDelay);
    }

    /// <summary>Validates the input: the base URL must be absolute http(s), the login URL is free text, numbers must not be negative and a starting page must not exceed its total pages.</summary>
    /// <returns>The validated <see cref="RootSettings"/>, or every validation error found.</returns>
    public Validation<RootSettings> Validate()
    {
        List<ValidationError> errors = [];
        var baseUrl = ParseUrl(nameof(BaseUrl), BaseUrl, errors);
        _ = RequireNotNegative(nameof(ImagePauseInSeconds), ImagePauseInSeconds, errors);
        RequirePages(nameof(StartingPageNumber), StartingPageNumber, nameof(TotalPages), TotalPages, errors);
        RequirePages(nameof(SubscriptionsStartingPageNumber), SubscriptionsStartingPageNumber, nameof(SubscriptionsTotalPages), SubscriptionsTotalPages, errors);
        RequirePages(nameof(TopWallpapersStartingPageNumber), TopWallpapersStartingPageNumber, nameof(TopWallpapersTotalPages), TopWallpapersTotalPages, errors);
        _ = SlowMotionDelay.Match(delay => RequireNotNegative(nameof(SlowMotionDelay), delay, errors), () => true);

        return errors.Count > 0
            ? Validation.Invalid<RootSettings>(errors)
            : Validation.Valid(new RootSettings(
                baseUrl,
                LoginUrl,
                ApiKey,
                SearchString,
                TopWallpapers,
                SearchStringPrefix,
                SearchStringSuffix,
                Subscriptions,
                ImagePauseInSeconds,
                StartingPageNumber,
                TotalPages,
                SubscriptionsStartingPageNumber,
                SubscriptionsTotalPages,
                TopWallpapersStartingPageNumber,
                TopWallpapersTotalPages,
                UseHeadless,
                SlowMotionDelay));
    }

    private static Uri ParseUrl(string property, string text, List<ValidationError> errors)
    {
        if (Uri.TryCreate(text, UriKind.Absolute, out var url) && url.Scheme is "http" or "https") return url;

        errors.Add(ValidationErrorFactory.Create(property, "Must be an absolute http or https URL."));

        return new Uri("https://example.com");
    }

    private static bool RequireNotNegative(string property, float value, List<ValidationError> errors)
    {
        if (value >= 0) return true;

        errors.Add(ValidationErrorFactory.Create(property, "Must not be negative."));

        return false;
    }

    private static void RequirePages(string startProperty, int start, string totalProperty, int total, List<ValidationError> errors)
    {
        var isValid = RequireNotNegative(startProperty, start, errors) & RequireNotNegative(totalProperty, total, errors);
        if (isValid && start > total) errors.Add(ValidationErrorFactory.Create(startProperty, "Must not exceed the total pages."));
    }
}
