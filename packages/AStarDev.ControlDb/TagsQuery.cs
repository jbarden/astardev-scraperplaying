using AStarDev.ControlDb.TagDetail;
using AStarDev.FunctionalParadigm;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ControlDb;

public interface ITagsQuery
{
    Task<Exceptional<Option<TagEntity>>> TryFindByWallhavenIdAsync(int wallhavenTagId, CancellationToken cancellationToken = default);
}

public class TagsQuery(ControlDbContext context) : ITagsQuery
{
    public Task<Exceptional<Option<TagEntity>>> TryFindByWallhavenIdAsync(int wallhavenTagId, CancellationToken cancellationToken = default)
            => Try.RunAsync(async () => (Option<TagEntity>)await context.Tags.FirstOrDefaultAsync(tag => tag.WallhavenTagId == wallhavenTagId, cancellationToken));
}
