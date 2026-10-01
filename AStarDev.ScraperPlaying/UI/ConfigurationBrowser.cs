using System.IO.Abstractions;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using Microsoft.Extensions.Logging;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>Lists and loads scrape configurations for the main window, reporting problems to the user instead of throwing, and creates the editor for a loaded configuration.</summary>
/// <param name="catalogue">The source of the configurations.</param>
/// <param name="saver">The service the editor validates and saves through.</param>
/// <param name="fileSystem">The file system the editor uses to check the scrape directories.</param>
/// <param name="status">Where problems are reported.</param>
/// <param name="logger">The logger the visit to the editor is written to.</param>
public sealed class ConfigurationBrowser(IScrapeConfigurationCatalogue catalogue, ConfigurationEditSaver saver, IFileSystem fileSystem, StatusReporter status, ILogger<ConfigurationBrowser> logger)
{
    /// <summary>Lists the available configurations; an empty list, with the failure reported, if they could not be listed.</summary>
    public async Task<IReadOnlyList<ScrapeConfigurationSummary>> ListAsync() =>
        (await catalogue.ListAsync()).Match(summaries => summaries, exception =>
        {
            status.Error("Unable to list scrape configurations.", exception);

            return [];
        });

    /// <summary>Loads the configuration behind <paramref name="summary"/> so it can be edited, logging the visit to the editor (never what is changed); none, with the reason reported, if it could not be loaded or no longer exists.</summary>
    /// <param name="summary">The configuration to load.</param>
    public async Task<Option<ScrapeConfigurationEntity>> FindAsync(ScrapeConfigurationSummary summary) =>
        (await catalogue.FindAsync(summary.Id)).Match(ReportIfMissing, exception =>
        {
            status.Error("Unable to load scrape configuration.", exception);

            return Option.None<ScrapeConfigurationEntity>();
        });

    /// <summary>Creates the editor window with every configuration selectable, opened on the first; none, with the reason reported, if there is nothing to edit.</summary>
    public async Task<Option<ConfigurationEditorWindow>> CreateEditorAsync()
    {
        var summaries = await ListAsync();
        if (summaries.Count == 0)
        {
            status.Append("There are no scrape configurations to edit.");

            return Option.None<ConfigurationEditorWindow>();
        }

        return (await FindAsync(summaries[0])) is Option<ScrapeConfigurationEntity>.Some found
            ? Option.Some(new ConfigurationEditorWindow(found.Value, summaries, this, saver, fileSystem))
            : Option.None<ConfigurationEditorWindow>();
    }

    private Option<ScrapeConfigurationEntity> ReportIfMissing(Option<ScrapeConfigurationEntity> found)
    {
        if (found is Option<ScrapeConfigurationEntity>.None) status.Append("The selected scrape configuration could not be found.");
        else LogMessage.PageView(logger, "Scrape configuration editor");

        return found;
    }
}
