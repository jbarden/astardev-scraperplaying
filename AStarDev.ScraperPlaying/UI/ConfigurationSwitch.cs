using System.Diagnostics.CodeAnalysis;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>Decides what happens when the person picks another configuration in the editor.</summary>
public static class ConfigurationSwitch
{
    /// <summary>Confirms the edits can be discarded, then loads the requested configuration.</summary>
    /// <param name="requested">The configuration the person picked.</param>
    /// <param name="canDiscardEdits">Asks whether the unsaved edits may be discarded.</param>
    /// <param name="session">How to load the requested configuration.</param>
    /// <returns>The loaded configuration, or none when the picker must go back to the configuration being edited, with the failure to show when there is one.</returns>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Called from an async void handler: any exception escaping it would crash the application, so the failure is shown in the editor instead.")]
    public static async Task<ConfigurationSwitchResult> ResolveAsync(ScrapeConfigurationSummary requested, Func<Task<bool>> canDiscardEdits, ConfigurationEditorSession session)
    {
        try
        {
            return await canDiscardEdits() ? new ConfigurationSwitchResult(await session.LoadAsync(requested), string.Empty) : ConfigurationSwitchResult.Stay;
        }
        catch (Exception exception)
        {
            return new ConfigurationSwitchResult(Option.None<LoadedConfiguration>(), $"Unable to load the scrape configuration. {exception.Message}");
        }
    }
}

/// <summary>The outcome of picking another configuration in the editor.</summary>
/// <param name="Loaded">The configuration to show, or none when the picker must go back to the configuration being edited.</param>
/// <param name="Error">The failure to show, or empty when there is none.</param>
public sealed record ConfigurationSwitchResult(Option<LoadedConfiguration> Loaded, string Error)
{
    /// <summary>Stay on the configuration being edited, with nothing to report.</summary>
    public static ConfigurationSwitchResult Stay { get; } = new(Option.None<LoadedConfiguration>(), string.Empty);
}
