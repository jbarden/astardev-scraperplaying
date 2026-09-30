using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>The status messages for the outcomes of importing and exporting a scrape configuration.</summary>
public static class ConfigurationTransferMessages
{
    /// <summary>Gets the message for an import outcome.</summary>
    /// <param name="outcome">Some when the import completed, none when the user did not complete it.</param>
    public static string ForImport(Option<Unit> outcome) => outcome.Match(
        _ => "Scrape configuration imported.",
        () => "Scrape configuration import could not be completed.");

    /// <summary>Gets the message for an export outcome.</summary>
    /// <param name="outcome">Some(true) when a configuration was written, some(false) when there was none to export, none when the user did not complete it.</param>
    public static string ForExport(Option<bool> outcome) => outcome.Match(
        exported => exported ? "Scrape configuration exported." : "No scrape configuration was found to export.",
        () => "Scrape configuration export could not be completed.");
}
