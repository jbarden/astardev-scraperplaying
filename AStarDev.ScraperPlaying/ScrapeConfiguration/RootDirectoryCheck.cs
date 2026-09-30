using System.IO.Abstractions;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <inheritdoc/>
public sealed class RootDirectoryCheck(IServiceScopeFactory scopeFactory, IFileSystem fileSystem) : IRootDirectoryCheck
{
    /// <inheritdoc/>
    public async Task<bool> ExistsAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var rootDirectory = (await scope.ServiceProvider.GetRequiredService<IScrapeConfigurationLookup>().TryGetRootDirectoryAsync())
            .Match(
                option => option.Match(directory => directory, () => throw new InvalidOperationException("Scrape configuration not found")),
                exception => throw exception);

        return fileSystem.Directory.Exists(rootDirectory);
    }
}
