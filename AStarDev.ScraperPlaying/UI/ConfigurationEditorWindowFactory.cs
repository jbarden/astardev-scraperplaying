using System.IO.Abstractions;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.ScraperPlaying.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>Creates the editor window for a loaded scrape configuration.</summary>
/// <param name="saver">The service the editor validates and saves through.</param>
/// <param name="fileSystem">The file system the editor uses to check the scrape directories.</param>
public sealed class ConfigurationEditorWindowFactory(ConfigurationEditSaver saver, IFileSystem fileSystem)
{
    /// <summary>Creates the editor opened on <paramref name="configuration"/>.</summary>
    /// <param name="configuration">The scrape configuration to edit first.</param>
    /// <param name="summaries">Every scrape configuration the user can switch to.</param>
    /// <param name="browser">The browser used to load the configuration the user switches to.</param>
    public ConfigurationEditorWindow Create(ScrapeConfigurationEntity configuration, IReadOnlyList<ScrapeConfigurationSummary> summaries, ConfigurationBrowser browser)
        => new(configuration, summaries, browser, saver, fileSystem);
}
