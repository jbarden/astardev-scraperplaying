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

        var ignoreFlagChanged = false;
        var saved = await scopedRunner.RunAsync<ITagsQuery, IUnitOfWork, Exceptional<Unit>>((query, unitOfWork) => Try.RunAsync(async () =>
        {
            var tags = (await query.FindByWallhavenIdsAsync([.. flagsByWallhavenId.Keys], cancellationToken)).GetOrThrow();
            foreach (var tag in tags)
            {
                var flags = flagsByWallhavenId[tag.WallhavenTagId];
                ignoreFlagChanged |= tag.IgnoreImage != flags.IgnoreImage;
                tag.IgnoreImage = flags.IgnoreImage;
                tag.IsName = flags.IsName;
                tag.IsFamous = flags.IsFamous;
            }

            _ = await unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Instance;
        }));

        return ignoreFlagChanged && saved is Success<Unit> ? await ForgetIgnoredWallpapersAsync(cancellationToken) : saved;
    }

    /// <summary>Forgets the remembered ignored wallpapers: one may have been ignored for a tag that is no longer flagged, so every one is evaluated again on the next scrape.</summary>
    private async Task<Exceptional<Unit>> ForgetIgnoredWallpapersAsync(CancellationToken cancellationToken) =>
        await scopedRunner.RunAsync<IIgnoredWallpapers, Exceptional<Unit>>(async ignoredWallpapers =>
            (await ignoredWallpapers.ForgetAllAsync(cancellationToken)).Match(
                _ => (Exceptional<Unit>)Unit.Instance,
                exception => exception));
}
