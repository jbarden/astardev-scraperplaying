using System.IO.Abstractions;
using AStarDev.ControlDb;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
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
            var directories = (await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().LoadScrapeConfigurationAsync()).ScrapeDirectories;
            var fileRecords = (await scope.ServiceProvider.GetRequiredService<IFileDetailsClearer>().ClearAsync(cancellationToken)).Match(count => count, exception => throw exception);

            fileSystem.EmptyDirectory(directories.RootDirectory);
            fileSystem.EmptyDirectory(directories.RootDirectoryFamous);
            _ = fileSystem.Directory.CreateDirectory(directories.RootDirectory);
            _ = fileSystem.Directory.CreateDirectory(directories.RootDirectoryFamous);

            return new ClearedDownloads(fileRecords);
        });
    }
}
