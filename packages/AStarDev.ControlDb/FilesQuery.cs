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
    public Task<Exceptional<IReadOnlyList<FileName>>> GetExistingNamesAsync(IReadOnlyCollection<FileName> names, CancellationToken cancellationToken = default)
            => Try.RunAsync<IReadOnlyList<FileName>>(async () =>
            {
                var values = names.Select(name => name.Value).ToList();

                return await context.Files
                                    .Where(file => values.Contains(file.FileName.Value))
                                    .Select(file => file.FileName)
                                    .ToListAsync(cancellationToken)
                                    .ConfigureAwait(false);
            });

    private static Expression<Func<FileEntity, bool>> HasName(FileName name) => file => file.FileName.Value == name.Value;
}
