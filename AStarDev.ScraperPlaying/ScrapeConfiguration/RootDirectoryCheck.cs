using System.IO.Abstractions;
using AStarDev.ControlDb;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <inheritdoc/>
public sealed class RootDirectoryCheck(IServiceScopeFactory scopeFactory, IFileSystem fileSystem) : IRootDirectoryCheck
{
    /// <inheritdoc/>
    public async Task<bool> ExistsAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var configuration = await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().LoadScrapeConfigurationAsync();

        return fileSystem.Directory.Exists(configuration.ScrapeDirectories.RootDirectory);
    }
}
