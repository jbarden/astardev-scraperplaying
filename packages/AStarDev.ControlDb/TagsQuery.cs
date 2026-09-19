using AStarDev.ControlDb.TagDetail;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb;

public interface ITagsQuery
{
    /// <summary>Finds the stored tags matching any of the given Wallhaven tag ids, in a single query.</summary>
    /// <param name="wallhavenTagIds">The Wallhaven tag ids to look for.</param>
    /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
    /// <returns>An exceptional result containing the tags that exist (ids with no stored tag are simply absent).</returns>
    Task<Exceptional<IReadOnlyList<TagEntity>>> FindByWallhavenIdsAsync(IReadOnlyCollection<int> wallhavenTagIds, CancellationToken cancellationToken = default);
}

public class TagsQuery(ControlDbContext context) : ITagsQuery
{
    public Task<Exceptional<IReadOnlyList<TagEntity>>> FindByWallhavenIdsAsync(IReadOnlyCollection<int> wallhavenTagIds, CancellationToken cancellationToken = default)
            => Try.RunAsync(async () =>
            {
                var requested = wallhavenTagIds.ToList();

                return (IReadOnlyList<TagEntity>)await context.Tags.Where(tag => requested.Contains(tag.WallhavenTagId)).ToListAsync(cancellationToken);
            });
}
