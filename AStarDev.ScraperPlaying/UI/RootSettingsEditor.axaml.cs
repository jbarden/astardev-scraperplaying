using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using Avalonia.Controls;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>Edits the root-level settings of a scrape configuration.</summary>
public partial class RootSettingsEditor : UserControl
{
    public RootSettingsEditor() => InitializeComponent();

    /// <summary>Fills the controls from the specified settings.</summary>
    /// <param name="input">The settings to display.</param>
    public void Load(RootSettingsInput input)
    {
        BaseUrlBox.Text = input.BaseUrl;
        LoginUrlBox.Text = input.LoginUrl;
        ApiKeyBox.Text = input.ApiKey;
        SearchStringBox.Text = input.SearchString;
        SearchStringPrefixBox.Text = input.SearchStringPrefix;
        SearchStringSuffixBox.Text = input.SearchStringSuffix;
        TopWallpapersBox.Text = input.TopWallpapers;
        SubscriptionsBox.Text = input.Subscriptions;
        ImagePauseInSecondsBox.Value = input.ImagePauseInSeconds;
        StartingPageNumberBox.Value = input.StartingPageNumber;
        TotalPagesBox.Value = input.TotalPages;
        SubscriptionsStartingPageNumberBox.Value = input.SubscriptionsStartingPageNumber;
        SubscriptionsTotalPagesBox.Value = input.SubscriptionsTotalPages;
        TopWallpapersStartingPageNumberBox.Value = input.TopWallpapersStartingPageNumber;
        TopWallpapersTotalPagesBox.Value = input.TopWallpapersTotalPages;
        UseHeadlessCheckBox.IsChecked = input.UseHeadless;
        SlowMotionDelayBox.Value = input.SlowMotionDelay.Match(delay => (decimal?)delay, () => null);
    }

    /// <summary>Reads the settings currently entered in the controls.</summary>
    public RootSettingsInput ReadInput() => new(
        BaseUrlBox.Text ?? string.Empty,
        LoginUrlBox.Text ?? string.Empty,
        ApiKeyBox.Text ?? string.Empty,
        SearchStringBox.Text ?? string.Empty,
        TopWallpapersBox.Text ?? string.Empty,
        SearchStringPrefixBox.Text ?? string.Empty,
        SearchStringSuffixBox.Text ?? string.Empty,
        SubscriptionsBox.Text ?? string.Empty,
        ToInt(ImagePauseInSecondsBox),
        ToInt(StartingPageNumberBox),
        ToInt(TotalPagesBox),
        ToInt(SubscriptionsStartingPageNumberBox),
        ToInt(SubscriptionsTotalPagesBox),
        ToInt(TopWallpapersStartingPageNumberBox),
        ToInt(TopWallpapersTotalPagesBox),
        UseHeadlessCheckBox.IsChecked == true,
        SlowMotionDelayBox.Value is { } delay ? Option.Some((float)delay) : Option.None<float>());

    private static int ToInt(NumericUpDown control) => (int)(control.Value ?? 0);
}
