using AStarDev.ControlDb.TagDetail;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb;

public interface ITagsQuery
{
    /// <summary>Finds, in a single query, the tags whose Wallhaven id is one of <paramref name="wallhavenTagIds"/>. Ids with no stored tag are simply absent from the result.</summary>
    /// <param name="wallhavenTagIds">The Wallhaven tag ids to look up.</param>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    Task<Exceptional<IReadOnlyList<TagEntity>>> FindByWallhavenIdsAsync(IReadOnlyCollection<int> wallhavenTagIds, CancellationToken cancellationToken = default);

    /// <summary>Lists every stored tag, ordered by name.</summary>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    Task<Exceptional<IReadOnlyList<TagEntity>>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets the Wallhaven ids of every tag flagged <see cref="TagEntity.IgnoreImage"/>.</summary>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    Task<Exceptional<IReadOnlyCollection<int>>> GetIgnoredWallhavenIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets the Wallhaven ids of every tag flagged <see cref="TagEntity.IsName"/>.</summary>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    Task<Exceptional<IReadOnlyCollection<int>>> GetNameWallhavenIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets the Wallhaven ids of every tag flagged <see cref="TagEntity.IsFamous"/>.</summary>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    Task<Exceptional<IReadOnlyCollection<int>>> GetFamousWallhavenIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets the Wallhaven ids of every stored tag.</summary>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    Task<Exceptional<IReadOnlyCollection<int>>> GetStoredWallhavenIdsAsync(CancellationToken cancellationToken = default);
}

public class TagsQuery(ControlDbContext context) : ITagsQuery
{
    public Task<Exceptional<IReadOnlyList<TagEntity>>> FindByWallhavenIdsAsync(IReadOnlyCollection<int> wallhavenTagIds, CancellationToken cancellationToken = default)
            => Try.RunAsync<IReadOnlyList<TagEntity>>(async () => await context.Tags
                            .Where(tag => wallhavenTagIds.Contains(tag.WallhavenTagId))
                            .ToListAsync(cancellationToken)
                            .ConfigureAwait(false));

    public Task<Exceptional<IReadOnlyList<TagEntity>>> ListAsync(CancellationToken cancellationToken = default)
            => Try.RunAsync<IReadOnlyList<TagEntity>>(async () => await context.Tags
                            .OrderBy(tag => tag.Name)
                            .ToListAsync(cancellationToken)
                            .ConfigureAwait(false));

    public Task<Exceptional<IReadOnlyCollection<int>>> GetIgnoredWallhavenIdsAsync(CancellationToken cancellationToken = default)
            => Try.RunAsync<IReadOnlyCollection<int>>(async () => await context.Tags
                            .Where(tag => tag.IgnoreImage)
                            .Select(tag => tag.WallhavenTagId)
                            .ToListAsync(cancellationToken)
                            .ConfigureAwait(false));

    public Task<Exceptional<IReadOnlyCollection<int>>> GetNameWallhavenIdsAsync(CancellationToken cancellationToken = default)
            => Try.RunAsync<IReadOnlyCollection<int>>(async () => await context.Tags
                            .Where(tag => tag.IsName)
                            .Select(tag => tag.WallhavenTagId)
                            .ToListAsync(cancellationToken)
                            .ConfigureAwait(false));

    public Task<Exceptional<IReadOnlyCollection<int>>> GetFamousWallhavenIdsAsync(CancellationToken cancellationToken = default)
            => Try.RunAsync<IReadOnlyCollection<int>>(async () => await context.Tags
                            .Where(tag => tag.IsFamous)
                            .Select(tag => tag.WallhavenTagId)
                            .ToListAsync(cancellationToken)
                            .ConfigureAwait(false));

    public Task<Exceptional<IReadOnlyCollection<int>>> GetStoredWallhavenIdsAsync(CancellationToken cancellationToken = default)
            => Try.RunAsync<IReadOnlyCollection<int>>(async () => await context.Tags
                            .Select(tag => tag.WallhavenTagId)
                            .ToListAsync(cancellationToken)
                            .ConfigureAwait(false));
}
