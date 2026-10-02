using System.Linq.Expressions;
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
                            .AsNoTracking()
                            .Include(file => file.DeletionStatus)
                            .Include(file => file.FileAccessDetail)
                            .Include(file => file.ImageDetail)
                            .Where(HasName(name))
                            .Take(1)
                            .AsAsyncEnumerable()
                            .FirstOrNoneAsync(cancellationToken)
                            .ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<Exceptional<bool>> CheckExistsByNameAsync(FileName name, CancellationToken cancellationToken = default)
            => await context.Files.AnyAsync(HasName(name), cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public Task<Exceptional<IReadOnlyList<FileHandle>>> GetExistingHandlesAsync(IReadOnlyCollection<FileHandle> fileHandles, CancellationToken cancellationToken = default)
            => Try.RunAsync<IReadOnlyList<FileHandle>>(async () =>
            {
                var stored = await context.Files
                                          .AsNoTracking()
                                          .Where(file => fileHandles.Contains(file.FileHandle))
                                          .Select(file => file.FileHandle)
                                          .ToListAsync(cancellationToken)
                                          .ConfigureAwait(false);
                var ignored = await context.IgnoredWallpapers
                                           .AsNoTracking()
                                           .Where(ignoredWallpaper => fileHandles.Contains(ignoredWallpaper.FileHandle))
                                           .Select(ignoredWallpaper => ignoredWallpaper.FileHandle)
                                           .ToListAsync(cancellationToken)
                                           .ConfigureAwait(false);

                return [.. stored, .. ignored];
            });

    private static Expression<Func<FileEntity, bool>> HasName(FileName name) => file => file.FileName.Value == name.Value;
}
