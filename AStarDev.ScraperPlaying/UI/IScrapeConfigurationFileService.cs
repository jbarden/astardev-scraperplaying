using AStarDev.FunctionalParadigm;
using Avalonia.Controls;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>Interface for importing/exporting the scrape configuration via a file picker anchored to a window.</summary>
public interface IScrapeConfigurationFileService
{
    /// <summary>Prompts the user via <paramref name="owner"/> to pick a file to import, then imports it.</summary>
    /// <param name="owner">The window to anchor the file picker dialog to.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, containing <see cref="Option{T}.Some"/> once imported, or <see cref="Option{T}.None"/> if the user cancelled the file picker.</returns>
    Task<Option<Unit>> ImportViaPickerAsync(Window owner, CancellationToken cancellationToken);

    /// <summary>Prompts the user via <paramref name="owner"/> to pick a destination file, then exports the current scrape configuration to it.</summary>
    /// <param name="owner">The window to anchor the file picker dialog to.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing <see cref="Option{T}.Some"/> wrapping whether
    /// a scrape configuration existed to export, or <see cref="Option{T}.None"/> if the user cancelled the file
    /// picker.
    /// </returns>
    Task<Option<bool>> ExportViaPickerAsync(Window owner, CancellationToken cancellationToken);
}
