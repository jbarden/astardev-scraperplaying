using System.IO.Abstractions;
using AStarDev.ControlDb;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.Utilities;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <inheritdoc/>
/// <remarks>Registered as Scoped (one scrape run): the root directories are loaded on first use and reused, rather than reloading the whole scrape configuration for every page set.</remarks>
public class SaveDirectoryResolver(IFileSystem fileSystem, IUnitOfWork unitOfWork) : ISaveDirectoryResolver
{
    private const string TopWallpapersDirectorySegment = "top-wallpapers";
    private Option<SaveDirectories> rootDirectories = Option.None<SaveDirectories>();

    /// <inheritdoc/>
    public async Task<SaveDirectories> ResolveSaveDirectoriesAsync(Option<string> categoryName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var directorySegment = categoryName.Match(name => name.ToDirectorySlug(), () => TopWallpapersDirectorySegment);
        var roots = await LoadRootDirectoriesAsync();

        return new SaveDirectories(fileSystem.Path.Combine(roots.Directory, directorySegment), fileSystem.Path.Combine(roots.FamousDirectory, directorySegment));
    }

    private async Task<SaveDirectories> LoadRootDirectoriesAsync()
    {
        if (rootDirectories is Option<SaveDirectories>.Some cached) return cached.Value;

        var scrapeDirectories = (await unitOfWork.LoadScrapeConfigurationAsync()).ScrapeDirectories;
        var loaded = new SaveDirectories(scrapeDirectories.RootDirectory, scrapeDirectories.RootDirectoryFamous);

        rootDirectories = Option.Some(loaded);

        return loaded;
    }
}
