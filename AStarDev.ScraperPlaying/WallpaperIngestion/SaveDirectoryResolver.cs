using AStarDev.ControlDb;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.Utilities;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <inheritdoc/>
/// <remarks>Registered as Scoped (one scrape run): the root directories are loaded on first use and reused, rather than reloading the whole scrape configuration for every page set.</remarks>
public class SaveDirectoryResolver(IUnitOfWork unitOfWork) : ISaveDirectoryResolver
{
    private const string TopWallpapersDirectorySegment = "top-wallpapers";
    private Option<RootDirectories> rootDirectories = Option.None<RootDirectories>();

    /// <inheritdoc/>
    public async Task<SaveDirectories> ResolveSaveDirectoriesAsync(Option<string> categoryName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var directorySegment = categoryName.Match(name => name.ToDirectorySlug(), () => TopWallpapersDirectorySegment);
        var roots = await LoadRootDirectoriesAsync();

        return new SaveDirectories(roots.Root, roots.FamousRoot, directorySegment);
    }

    private async Task<RootDirectories> LoadRootDirectoriesAsync()
    {
        if (rootDirectories is Option<RootDirectories>.Some cached) return cached.Value;

        var scrapeDirectories = (await unitOfWork.LoadScrapeConfigurationAsync()).ScrapeDirectories;
        var loaded = new RootDirectories(scrapeDirectories.RootDirectory, scrapeDirectories.RootDirectoryFamous);

        rootDirectories = Option.Some(loaded);

        return loaded;
    }

    private sealed record RootDirectories(string Root, string FamousRoot);
}
