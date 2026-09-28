using AStarDev.ControlDb.TagDetail;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ControlDb;

public interface ITagsQuery
{
    Task<Exceptional<Option<TagEntity>>> TryFindByWallhavenIdAsync(int wallhavenTagId, CancellationToken cancellationToken = default);
}

public class TagsQuery(ControlDbContext context) : ITagsQuery
{
    public async Task<Exceptional<Option<TagEntity>>> TryFindByWallhavenIdAsync(int wallhavenTagId, CancellationToken cancellationToken = default)
            => await context.Tags
                            .AsAsyncEnumerable()
                            .FirstOrNoneAsync(tag => tag.WallhavenTagId == wallhavenTagId, cancellationToken);
}
