using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.SearchAPI;
using Avalonia.Controls;

namespace AStarDev.ScraperPlaying.Home;

/// <inheritdoc/>
public sealed class ScrapeConfigurationFileService(IScrapeConfigurationImportService importService, IScrapeConfigurationExportService exportService, IConfigurationFilePicker configurationFilePicker) : IScrapeConfigurationFileService
{
    /// <inheritdoc/>
    public async Task<Option<UnitFp>> ImportViaPickerAsync(Window owner, CancellationToken cancellationToken)
    {
        var path = await configurationFilePicker.PickAsync(owner);

        return await path.MatchAsync(
            async selectedPath =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await importService.ImportAsync(selectedPath, cancellationToken);

                return Option.Some(UnitFp.Instance);
            },
            Option.None<UnitFp>);
    }

    /// <inheritdoc/>
    public async Task<Option<bool>> ExportViaPickerAsync(Window owner, CancellationToken cancellationToken)
    {
        var path = await configurationFilePicker.PickSaveAsync(owner);

        return await path.MatchAsync(
            async selectedPath =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var exported = await exportService.ExportAsync(selectedPath, cancellationToken);

                return Option.Some(exported);
            },
            Option.None<bool>);
    }
}
