using AStarDev.FunctionalParadigm;

namespace AStarDev.ControlDb;

/// <summary>Clears every stored file record.</summary>
public interface IFileDetailsClearer
{
    /// <summary>Removes every row from the file detail table, along with the rows that depend on them (file access detail, image detail, deletion status and file/tag links). Tags themselves and every other table are left alone.</summary>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    /// <returns>An exceptional result containing the number of file records removed.</returns>
    Task<Exceptional<int>> ClearAsync(CancellationToken cancellationToken = default);
}
