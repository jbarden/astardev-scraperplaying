using System.IO.Abstractions;
using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.Utilities;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <inheritdoc/>
public class SaveDirectoryResolver(IFileSystem fileSystem, IUnitOfWork unitOfWork) : ISaveDirectoryResolver
{
    private const string TopWallpapersDirectorySegment = "top-wallpapers";

    /// <inheritdoc/>
    public async Task<string> ResolveSaveDirectoryAsync(Option<string> categoryName, bool isFamous, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var directorySegment = categoryName.Match(name => name.ToDirectorySlug(), () => TopWallpapersDirectorySegment);
        var rootDirectory = await LoadRootDirectoryAsync(isFamous);

        return fileSystem.Path.Combine(rootDirectory, directorySegment);
    }

    private async Task<string> LoadRootDirectoryAsync(bool isFamous)
    {
        var configuration = (await unitOfWork.GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>().TryGetFirstAsync())
            .Match(
                option => option.Match(scrapeConfig => scrapeConfig, () => throw new InvalidOperationException("Scrape configuration not found")),
                exception => throw exception
            );

        if (!isFamous) return configuration.ScrapeDirectories.RootDirectory;

        return string.IsNullOrWhiteSpace(configuration.ScrapeDirectories.RootDirectoryFamous)
            ? throw new InvalidOperationException("The famous root directory is not configured.")
            : configuration.ScrapeDirectories.RootDirectoryFamous;
    }
}
