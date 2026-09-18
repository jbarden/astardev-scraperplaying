using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb;

/// <summary>Implementation of the IFilesQuery interface for querying files in the database.</summary>
/// <param name="context">The database context used for querying files.</param>
public class FilesQuery(ControlDbContext context) : IFilesQuery
{
    /// <inheritdoc/>
    public async Task<Exceptional<Option<FileEntity>>> TryGetByNameAsync(FileName name, CancellationToken cancellationToken = default)
            => await context.Files
                            .Include(f => f.DeletionStatus)
                            .Include(f => f.FileAccessDetail)
                            .Include(f => f.ImageDetail)
                            .AsAsyncEnumerable()
                            .FirstOrNoneAsync(f => f.FileName.Value.Contains(name.Value), cancellationToken);

    /// <inheritdoc/>
    public async Task<Exceptional<bool>> CheckExistsByNameAsync(FileName name, CancellationToken cancellationToken = default)
            => await context.Files.AnyAsync(f => f.FileName.Value.Contains(name.Value), cancellationToken);

    /// <inheritdoc/>
    public async Task<Exceptional<bool>> CheckExistsByHandleAsync(FileHandle handle, CancellationToken cancellationToken = default)
            => await context.Files.AnyAsync(f => f.FileHandle == handle, cancellationToken);
}
