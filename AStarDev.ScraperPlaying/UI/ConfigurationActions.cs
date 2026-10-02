using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>The scrape configuration actions the main window's menus trigger: import, export and edit.</summary>
public sealed class ConfigurationActions(
    UserOperationRunner operations,
    StatusReporter status,
    IScrapeConfigurationImportService importService,
    IScrapeConfigurationExportService exportService,
    ConfigurationBrowser configurationBrowser)
{
    /// <summary>Imports a scrape configuration from a file the person picks.</summary>
    /// <param name="dialogs">The dialogs the person is asked through.</param>
    public async Task ImportConfigurationAsync(IDialogHost dialogs)
        => await operations.RunAsync("Scrape configuration import cancelled.", "Unable to import scrape configuration.", async cancellationToken =>
            status.Append(ConfigurationTransferMessages.ForImport(await ImportPickedFileAsync(dialogs, cancellationToken))));

    /// <summary>Exports the scrape configuration to a file the person picks.</summary>
    /// <param name="dialogs">The dialogs the person is asked through.</param>
    /// <param name="apiKeys">Whether the exported file includes the API keys.</param>
    public async Task ExportConfigurationAsync(IDialogHost dialogs, ApiKeyExport apiKeys)
        => await operations.RunAsync("Scrape configuration export cancelled.", "Unable to export scrape configuration.", async cancellationToken =>
            status.Append(ConfigurationTransferMessages.ForExport(await ExportPickedFileAsync(dialogs, apiKeys, cancellationToken), apiKeys)));

    /// <summary>Shows the scrape configuration editor.</summary>
    /// <param name="dialogs">The dialogs the person is asked through.</param>
    public async Task EditConfigurationAsync(IDialogHost dialogs)
        => await operations.ReportFailuresAsync("Unable to edit scrape configuration.", async () =>
        {
            if (await configurationBrowser.CreateEditorAsync() is Option<ConfigurationEditorWindow>.Some editor) await dialogs.ShowAsync(editor.Value);
        });

    private async Task<Option<Unit>> ImportPickedFileAsync(IDialogHost dialogs, CancellationToken cancellationToken)
    {
        var path = await dialogs.PickImportFileAsync();

        return await path.MatchAsync(
            async selectedPath =>
            {
                await importService.ImportAsync(selectedPath, cancellationToken);

                return Option.Some(Unit.Instance);
            },
            Option.None<Unit>);
    }

    private async Task<Option<bool>> ExportPickedFileAsync(IDialogHost dialogs, ApiKeyExport apiKeys, CancellationToken cancellationToken)
    {
        var path = await dialogs.PickExportFileAsync();

        return await path.MatchAsync(
            async selectedPath => Option.Some(await exportService.ExportAsync(selectedPath, apiKeys, cancellationToken)),
            Option.None<bool>);
    }
}
