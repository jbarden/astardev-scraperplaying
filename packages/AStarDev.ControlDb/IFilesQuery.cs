using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ControlDb;

/// <summary>Provides methods for querying files in the database by their name.</summary>
public interface IFilesQuery
{
    /// <summary>Tries to get a file entity by its name from the database. Returns an exceptional result containing an option of the file entity if found.</summary>
    /// <param name="name">The name of the file to search for.</param>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    /// <returns>An exceptional result containing an option of the file entity if found.</returns>
    Task<Exceptional<Option<FileEntity>>> TryGetByNameAsync(FileName name, CancellationToken cancellationToken = default);

    /// <summary>Checks if a file with the specified name exists in the database. Returns an exceptional result containing a boolean indicating existence.</summary>
    /// <param name="name">The name of the file to check for existence.</param>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    /// <returns>An exceptional result containing a boolean indicating whether the file exists.</returns>
    Task<Exceptional<bool>> CheckExistsByNameAsync(FileName name, CancellationToken cancellationToken = default);

    /// <summary>Gets the handles among those supplied that identify a stored file or a wallpaper remembered as ignored (see <see cref="IIgnoredWallpapers"/>), so neither is fetched again. Handles are matched using the database collation, so the returned handles may differ in case from those supplied.</summary>
    /// <param name="fileHandles">The handles of the files to search for. A file's handle is stable across renames, unlike its file name.</param>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    /// <returns>An exceptional result containing the stored handles of the files that exist and of the wallpapers remembered as ignored.</returns>
    Task<Exceptional<IReadOnlyList<FileHandle>>> GetExistingHandlesAsync(IReadOnlyCollection<FileHandle> fileHandles, CancellationToken cancellationToken = default);
}
