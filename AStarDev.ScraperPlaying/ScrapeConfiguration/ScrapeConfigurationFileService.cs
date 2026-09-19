using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <inheritdoc/>
/// <remarks>Singleton: the import and export services use the scoped database context, so each operation resolves them from a scope of its own.</remarks>
public sealed class ScrapeConfigurationFileService(IServiceScopeFactory scopeFactory, IConfigurationFilePicker configurationFilePicker) : IScrapeConfigurationFileService
{
    /// <inheritdoc/>
    public async Task<Option<Unit>> ImportViaPickerAsync(Window owner, CancellationToken cancellationToken)
    {
        var path = await configurationFilePicker.PickAsync(owner);

        return await path.MatchAsync(
            async selectedPath =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IScrapeConfigurationImportService>().ImportAsync(selectedPath, cancellationToken);

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
                await using var scope = scopeFactory.CreateAsyncScope();
                var exported = await scope.ServiceProvider.GetRequiredService<IScrapeConfigurationExportService>().ExportAsync(selectedPath, cancellationToken);

                return Option.Some(exported);
            },
            Option.None<bool>);
    }
}
