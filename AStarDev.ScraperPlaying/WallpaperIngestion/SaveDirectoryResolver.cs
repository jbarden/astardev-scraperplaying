using System.IO.Abstractions;
using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.Utilities;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <inheritdoc/>
/// <remarks>Registered as Scoped (one scrape run): the root directory is loaded on first use and reused, rather than reloading the whole scrape configuration for every page set.</remarks>
public class SaveDirectoryResolver(IFileSystem fileSystem, IUnitOfWork unitOfWork) : ISaveDirectoryResolver
{
    private const string TopWallpapersDirectorySegment = "top-wallpapers";
    private Option<string> rootDirectory = Option.None<string>();

    /// <inheritdoc/>
    public async Task<string> ResolveSaveDirectoryAsync(Option<string> categoryName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var directorySegment = categoryName.Match(name => name.ToDirectorySlug(), () => TopWallpapersDirectorySegment);
        var rootDirectory = await LoadRootDirectoryAsync();

        return fileSystem.Path.Combine(rootDirectory, directorySegment);
    }

    private async Task<string> LoadRootDirectoryAsync()
    {
        if (rootDirectory is Option<string>.Some cached) return cached.Value;

        var configuration = (await unitOfWork.GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>().TryGetFirstAsync())
            .Match(
                option => option.Match(scrapeConfig => scrapeConfig, () => throw new InvalidOperationException("Scrape configuration not found")),
                exception => throw exception
            );

        rootDirectory = Option.Some(configuration.ScrapeDirectories.RootDirectory);

        return configuration.ScrapeDirectories.RootDirectory;
    }
}
