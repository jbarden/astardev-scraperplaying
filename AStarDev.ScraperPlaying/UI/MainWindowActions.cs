using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using Avalonia.Controls;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>The actions the main window's menus and buttons trigger, grouped so the window only has to translate control events into calls.</summary>
public sealed class MainWindowActions(
    UserOperationRunner operations,
    StatusReporter status,
    IScrapeConfigurationFileService scrapeConfigurationFileService,
    ConfigurationBrowser configurationBrowser,
    TagsBrowser tagsBrowser,
    ClearDownloadsRunner clearDownloadsRunner,
    ScrapeRunner scrapeRunner)
{
    /// <summary>Imports a scrape configuration from a file the person picks.</summary>
    /// <param name="owner">The window that owns the file picker.</param>
    public async Task ImportConfigurationAsync(Window owner)
        => await operations.RunAsync("Scrape configuration import cancelled.", "Unable to import scrape configuration.", async cancellationToken =>
            status.Append(ConfigurationTransferMessages.ForImport(await scrapeConfigurationFileService.ImportViaPickerAsync(owner, cancellationToken))));

    /// <summary>Exports the scrape configuration to a file the person picks.</summary>
    /// <param name="owner">The window that owns the file picker.</param>
    /// <param name="apiKeys">Whether the exported file includes the API keys.</param>
    public async Task ExportConfigurationAsync(Window owner, ApiKeyExport apiKeys)
        => await operations.RunAsync("Scrape configuration export cancelled.", "Unable to export scrape configuration.", async cancellationToken =>
            status.Append(ConfigurationTransferMessages.ForExport(await scrapeConfigurationFileService.ExportViaPickerAsync(owner, apiKeys, cancellationToken), apiKeys)));

    /// <summary>Shows the scrape configuration editor.</summary>
    /// <param name="owner">The window that owns the dialog.</param>
    public async Task EditConfigurationAsync(Window owner)
        => await operations.ReportFailuresAsync("Unable to edit scrape configuration.", async () =>
        {
            if (await configurationBrowser.CreateEditorAsync() is Option<ConfigurationEditorWindow>.Some editor) _ = await editor.Value.ShowDialog<bool>(owner);
        });

    /// <summary>Shows the tags editor.</summary>
    /// <param name="owner">The window that owns the dialog.</param>
    public async Task EditTagsAsync(Window owner)
        => await operations.ReportFailuresAsync("Unable to edit tags.", async () =>
        {
            if (await tagsBrowser.CreateEditorAsync() is Option<TagsEditorWindow>.Some editor) _ = await editor.Value.ShowDialog<bool>(owner);
        });

    /// <summary>Clears the downloads after the person confirms.</summary>
    /// <param name="owner">The window that owns the confirmation dialog.</param>
    public async Task ClearDownloadsAsync(Window owner)
        => await operations.ReportFailuresAsync("Unable to clear downloads.", async () =>
        {
            var confirmation = new ConfirmationWindow(
                "Clear Downloads",
                "This permanently deletes every file record and everything inside the base save and famous directories. Nothing else is affected. This cannot be undone. Continue?",
                "Clear Downloads");
            if (await confirmation.ShowDialog<bool>(owner)) await clearDownloadsRunner.RunAsync();
        });

    /// <summary>Runs the scraper.</summary>
    public async Task RunScraperAsync() => await scrapeRunner.RunAsync();

    /// <summary>Cancels the operation in progress.</summary>
    public void CancelOperation() => operations.Cancel();
}
