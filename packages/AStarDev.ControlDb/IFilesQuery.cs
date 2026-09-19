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

    /// <summary>Finds which of the specified handles already belong to a file in the database, in a single query.</summary>
    /// <param name="fileHandles">The handles to look for.</param>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    /// <returns>An exceptional result containing the subset of <paramref name="fileHandles"/> that exist.</returns>
    Task<Exceptional<IReadOnlySet<FileHandle>>> GetExistingHandlesAsync(IReadOnlyCollection<FileHandle> fileHandles, CancellationToken cancellationToken = default);
}
