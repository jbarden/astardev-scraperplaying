using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb;

public interface IFilesQuery
{
    Task<Exceptional<Option<FileEntity>>> TryGetByNameAsync(FileName name, CancellationToken cancellationToken = default);
    Task<Exceptional<bool>> CheckExistsByNameAsync(FileName name, CancellationToken cancellationToken = default);
}

public class FilesQuery(ControlDbContext context) : IFilesQuery
{
    public async Task<Exceptional<Option<FileEntity>>> TryGetByNameAsync(FileName name, CancellationToken cancellationToken = default)
            => await context.Files
                            .Include(f => f.DeletionStatus)
                            .Include(f => f.FileAccessDetail)
                            .Include(f => f.ImageDetail)
                            .AsAsyncEnumerable()
                            .FirstOrNoneAsync(f => f.FileName.Value.Contains(name.Value), cancellationToken);

    public async Task<Exceptional<bool>> CheckExistsByNameAsync(FileName name, CancellationToken cancellationToken = default)
            => await context.Files.AnyAsync(f => f.FileName.Value.Contains(name.Value), cancellationToken);
}
