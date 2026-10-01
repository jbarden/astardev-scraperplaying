using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using Avalonia.Controls;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>The <see cref="IDialogHost"/> that anchors every dialog to a window.</summary>
/// <param name="owner">The window that owns the dialogs.</param>
public sealed class WindowDialogHost(Window owner) : IDialogHost
{
    /// <inheritdoc/>
    public Task<Option<Unit>> ImportConfigurationAsync(IScrapeConfigurationFileService files, CancellationToken cancellationToken)
        => files.ImportViaPickerAsync(owner, cancellationToken);

    /// <inheritdoc/>
    public Task<Option<bool>> ExportConfigurationAsync(IScrapeConfigurationFileService files, ApiKeyExport apiKeys, CancellationToken cancellationToken)
        => files.ExportViaPickerAsync(owner, apiKeys, cancellationToken);

    /// <inheritdoc/>
    public async Task ShowAsync(Window dialog) => _ = await dialog.ShowDialog<bool>(owner);

    /// <inheritdoc/>
    public Task<bool> ConfirmAsync(string title, string message, string confirmLabel)
        => new ConfirmationWindow(title, message, confirmLabel).ShowDialog<bool>(owner);
}
