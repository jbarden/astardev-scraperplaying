using AStarDev.ControlDb;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scoping;

namespace AStarDev.ScraperPlaying.Tags;

/// <inheritdoc/>
public sealed class TagCatalogue(IScopedRunner scopedRunner) : ITagCatalogue
{
    /// <inheritdoc/>
    public async Task<Exceptional<IReadOnlyList<TagSummary>>> ListAsync(CancellationToken cancellationToken = default) =>
        await scopedRunner.RunAsync<ITagsQuery, Exceptional<IReadOnlyList<TagSummary>>>(async query =>
            (await query.ListAsync(cancellationToken)).Match(
                tags => (Exceptional<IReadOnlyList<TagSummary>>)new Success<IReadOnlyList<TagSummary>>([.. tags.Select(TagSummary.From)]),
                exception => exception));

    /// <inheritdoc/>
    public async Task<Exceptional<Unit>> SaveFlagsAsync(IReadOnlyDictionary<int, TagFlags> flagsByWallhavenId, CancellationToken cancellationToken = default)
    {
        if (flagsByWallhavenId.Count == 0) return Unit.Instance;

        return await scopedRunner.RunAsync<ITagsQuery, IUnitOfWork, Exceptional<Unit>>((query, unitOfWork) => Try.RunAsync(async () =>
        {
            var tags = (await query.FindByWallhavenIdsAsync([.. flagsByWallhavenId.Keys], cancellationToken)).GetOrThrow();
            foreach (var tag in tags)
            {
                var flags = flagsByWallhavenId[tag.WallhavenTagId];
                tag.IgnoreImage = flags.IgnoreImage;
                tag.IsName = flags.IsName;
                tag.IsFamous = flags.IsFamous;
            }

            _ = await unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Instance;
        }));
    }
}
