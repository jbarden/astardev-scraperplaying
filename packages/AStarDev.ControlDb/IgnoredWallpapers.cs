using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb;

/// <summary>Implementation of the <see cref="IIgnoredWallpapers"/> interface, tracking the handles on the shared database context.</summary>
/// <param name="context">The database context the page's other changes are saved through.</param>
public class IgnoredWallpapers(ControlDbContext context) : IIgnoredWallpapers
{
    /// <inheritdoc/>
    public Exceptional<Unit> Record(FileHandle fileHandle)
        => Try.Run(() =>
        {
            if (!IsTracked(fileHandle)) _ = context.IgnoredWallpapers.Add(new IgnoredWallpaperEntity { FileHandle = fileHandle });

            return Unit.Instance;
        });

    /// <inheritdoc/>
    public Task<Exceptional<int>> ForgetAllAsync(CancellationToken cancellationToken = default)
        => Try.RunAsync(() => context.IgnoredWallpapers.ExecuteDeleteAsync(cancellationToken));

    private bool IsTracked(FileHandle fileHandle)
        => context.IgnoredWallpapers.Local.Any(ignored => string.Equals(ignored.FileHandle.Value, fileHandle.Value, StringComparison.OrdinalIgnoreCase));
}
