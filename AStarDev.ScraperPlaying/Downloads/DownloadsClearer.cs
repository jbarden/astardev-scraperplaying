using System.IO.Abstractions;
using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scoping;

namespace AStarDev.ScraperPlaying.Downloads;

/// <inheritdoc/>
public sealed class DownloadsClearer(IScopedRunner scopedRunner, IFileSystem fileSystem) : IDownloadsClearer
{
    /// <inheritdoc/>
    public async Task<Exceptional<ClearedDownloads>> ClearAsync(CancellationToken cancellationToken = default)
    {
        return await scopedRunner.RunAsync<IScrapeConfigurationLookup, IFileDetailsClearer, Exceptional<ClearedDownloads>>((lookup, detailsClearer) => Try.RunAsync(async () =>
        {
            var directories = (await lookup.TryGetRootDirectoriesAsync(cancellationToken))
                .Match(found => found, exception => throw exception)
                .Match(found => found, () => throw new InvalidOperationException("No scrape configuration exists."));
            var fileRecords = (await detailsClearer.ClearAsync(cancellationToken)).Match(count => count, exception => throw exception);

            fileSystem.EmptyDirectory(directories.Root);
            fileSystem.EmptyDirectory(directories.FamousRoot);
            _ = fileSystem.Directory.CreateDirectory(directories.Root);
            _ = fileSystem.Directory.CreateDirectory(directories.FamousRoot);

            return new ClearedDownloads(fileRecords);
        }));
    }
}
