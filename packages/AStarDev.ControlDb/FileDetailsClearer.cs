using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb;

/// <summary>Implementation of the <see cref="IFileDetailsClearer"/> interface, relying on the cascade delete of the dependent rows.</summary>
/// <param name="context">The database context holding the file records.</param>
public class FileDetailsClearer(ControlDbContext context) : IFileDetailsClearer
{
    /// <inheritdoc/>
    public Task<Exceptional<int>> ClearAsync(CancellationToken cancellationToken = default)
        => Try.RunAsync(() => context.Files.ExecuteDeleteAsync(cancellationToken));
}
