using System.IO.Abstractions;
using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.ScraperPlaying.Downloads;

/// <inheritdoc/>
public sealed class DownloadsClearer(IServiceScopeFactory scopeFactory, IFileSystem fileSystem) : IDownloadsClearer
{
    /// <inheritdoc/>
    public async Task<Exceptional<ClearedDownloads>> ClearAsync(CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();

        return await Try.RunAsync(async () =>
        {
            var directories = (await scope.ServiceProvider.GetRequiredService<IScrapeConfigurationLookup>().TryGetRootDirectoriesAsync(cancellationToken))
                .Match(found => found, exception => throw exception)
                .Match(found => found, () => throw new InvalidOperationException("No scrape configuration exists."));
            var fileRecords = (await scope.ServiceProvider.GetRequiredService<IFileDetailsClearer>().ClearAsync(cancellationToken)).Match(count => count, exception => throw exception);

            fileSystem.EmptyDirectory(directories.Root);
            fileSystem.EmptyDirectory(directories.FamousRoot);
            _ = fileSystem.Directory.CreateDirectory(directories.Root);
            _ = fileSystem.Directory.CreateDirectory(directories.FamousRoot);

            return new ClearedDownloads(fileRecords);
        });
    }
}
