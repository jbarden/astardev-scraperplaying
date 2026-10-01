using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using Avalonia.Controls;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>The window-bound dialogs the main window's actions need, so the actions' logic does not depend on a live window.</summary>
public interface IDialogHost
{
    /// <summary>Lets the person pick a scrape configuration file and imports it.</summary>
    /// <param name="files">The service that performs the import.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    Task<Option<Unit>> ImportConfigurationAsync(IScrapeConfigurationFileService files, CancellationToken cancellationToken);

    /// <summary>Lets the person pick a destination file and exports the scrape configuration to it.</summary>
    /// <param name="files">The service that performs the export.</param>
    /// <param name="apiKeys">Whether the exported file includes the API keys.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    Task<Option<bool>> ExportConfigurationAsync(IScrapeConfigurationFileService files, ApiKeyExport apiKeys, CancellationToken cancellationToken);

    /// <summary>Shows <paramref name="dialog"/> modally.</summary>
    /// <param name="dialog">The dialog to show.</param>
    Task ShowAsync(Window dialog);

    /// <summary>Asks the person to confirm an action.</summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">What will happen if confirmed.</param>
    /// <param name="confirmLabel">The label of the confirm button.</param>
    /// <returns>Whether the person confirmed.</returns>
    Task<bool> ConfirmAsync(string title, string message, string confirmLabel);
}
