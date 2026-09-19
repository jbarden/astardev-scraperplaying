using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb;

/// <summary>Implementation of the IFilesQuery interface for querying files in the database.</summary>
/// <param name="context">The database context used for querying files.</param>
public class FilesQuery(ControlDbContext context) : IFilesQuery
{
    /// <inheritdoc/>
    public Task<Exceptional<Option<FileEntity>>> TryGetByNameAsync(FileName name, CancellationToken cancellationToken = default)
            => Try.RunAsync(async () => (Option<FileEntity>)await context.Files
                            .Include(f => f.DeletionStatus)
                            .Include(f => f.FileAccessDetail)
                            .Include(f => f.ImageDetail)
                            .FirstOrDefaultAsync(f => f.FileName.Value == name.Value, cancellationToken));

    /// <inheritdoc/>
    public async Task<Exceptional<bool>> CheckExistsByNameAsync(FileName name, CancellationToken cancellationToken = default)
            => await context.Files.AnyAsync(f => f.FileName.Value == name.Value, cancellationToken);

    /// <inheritdoc/>
    public Task<Exceptional<IReadOnlySet<FileHandle>>> GetExistingHandlesAsync(IReadOnlyCollection<FileHandle> fileHandles, CancellationToken cancellationToken = default)
            => Try.RunAsync(async () =>
            {
                var requested = fileHandles.ToList();
                var existing = await context.Files
                    .Where(f => requested.Contains(f.FileHandle))
                    .Select(f => f.FileHandle)
                    .ToListAsync(cancellationToken);

                return (IReadOnlySet<FileHandle>)existing.ToHashSet();
            });
}
