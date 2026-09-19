using System.IO.Abstractions;
using AStarDev.ControlDb;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.Utilities;

namespace AStarDev.ScraperPlaying.WallpaperIngestion;

/// <inheritdoc/>
/// <remarks>Scoped: the configured root directories are loaded once and reused for every wallpaper resolved during the scope (one scrape run).</remarks>
public class SaveDirectoryResolver(IFileSystem fileSystem, IUnitOfWork unitOfWork) : ISaveDirectoryResolver
{
    private const string TopWallpapersDirectorySegment = "top-wallpapers";

    private Option<Task<ScrapeDirectoriesEntity>> directoriesLoad = Option.None<Task<ScrapeDirectoriesEntity>>();

    /// <inheritdoc/>
    public async Task<string> ResolveSaveDirectoryAsync(Option<string> categoryName, bool isFamous, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var directorySegment = categoryName.Match(name => name.ToDirectorySlug(), () => TopWallpapersDirectorySegment);
        var rootDirectory = SelectRootDirectory(await GetDirectoriesAsync(), isFamous);

        return fileSystem.Path.Combine(rootDirectory, directorySegment);
    }

    private static string SelectRootDirectory(ScrapeDirectoriesEntity directories, bool isFamous)
    {
        if (!isFamous) return directories.RootDirectory;

        return string.IsNullOrWhiteSpace(directories.RootDirectoryFamous)
            ? throw new InvalidOperationException("The famous root directory is not configured.")
            : directories.RootDirectoryFamous;
    }

    private Task<ScrapeDirectoriesEntity> GetDirectoriesAsync()
        => directoriesLoad.Match(
            load => load,
            () =>
            {
                var load = LoadDirectoriesAsync();
                directoriesLoad = Option.Some(load);

                return load;
            });

    private async Task<ScrapeDirectoriesEntity> LoadDirectoriesAsync()
        => (await ScrapeConfigurationLoader.LoadAsync(unitOfWork)).ScrapeDirectories;
}
