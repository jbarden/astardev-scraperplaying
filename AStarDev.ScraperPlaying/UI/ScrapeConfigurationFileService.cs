using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using Avalonia.Controls;

namespace AStarDev.ScraperPlaying.UI;

/// <inheritdoc/>
public sealed class ScrapeConfigurationFileService(IScrapeConfigurationTransferService transferService, IConfigurationFilePicker configurationFilePicker) : IScrapeConfigurationFileService
{
    /// <inheritdoc/>
    public async Task<Option<Unit>> ImportViaPickerAsync(Window owner, CancellationToken cancellationToken)
    {
        var path = await configurationFilePicker.PickAsync(owner);

        return await path.MatchAsync(
            async selectedPath =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await transferService.ImportAsync(selectedPath, cancellationToken);

                return Option.Some(Unit.Instance);
            },
            Option.None<Unit>);
    }

    /// <inheritdoc/>
    public async Task<Option<bool>> ExportViaPickerAsync(Window owner, CancellationToken cancellationToken)
    {
        var path = await configurationFilePicker.PickSaveAsync(owner);

        return await path.MatchAsync(
            async selectedPath =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var exported = await transferService.ExportAsync(selectedPath, cancellationToken);

                return Option.Some(exported);
            },
            Option.None<bool>);
    }
}
