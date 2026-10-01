using System.IO.Abstractions;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scoping;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <inheritdoc/>
public sealed class RootDirectoryCheck(IScopedRunner scopedRunner, IFileSystem fileSystem) : IRootDirectoryCheck
{
    /// <inheritdoc/>
    public async Task<bool> ExistsAsync()
    {
        var rootDirectory = (await scopedRunner.RunAsync<IScrapeConfigurationLookup, Exceptional<Option<string>>>(lookup => lookup.TryGetRootDirectoryAsync()))
            .Match(
                option => option.Match(directory => directory, () => throw new InvalidOperationException("Scrape configuration not found")),
                exception => throw exception);

        return fileSystem.Directory.Exists(rootDirectory);
    }
}
