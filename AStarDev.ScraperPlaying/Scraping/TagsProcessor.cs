using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.ControlDb.TagDetail;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.Scraping;

/// <inheritdoc/>
public class TagsProcessor(ITagsQuery tagsQuery, IUnitOfWork unitOfWork, IFileTagRepository fileTagRepository) : ITagsProcessor
{
    /// <summary>
    /// Caches resolved tags for the lifetime of this instance (one scrape run - <see cref="TagsProcessor"/> is
    /// Scoped). Wallpaper ingestion batches <c>SaveChangesAsync</c> once per page, so without this,
    /// two different wallpapers on the same page introducing the same new tag would both miss
    /// <see cref="ITagsQuery.FindByWallhavenIdsAsync"/>'s database fast-path (the first one's insert isn't
    /// saved yet) and both try to add a duplicate <see cref="TagEntity"/>, failing the whole page's save on the
    /// unique-index violation.
    /// </summary>
    private readonly Dictionary<int, TagEntity> resolvedTags = [];

    /// <summary>Tags created since the last <see cref="AcceptPendingTags"/>: not stored until the page is saved, and dropped if that save fails.</summary>
    private readonly Dictionary<int, TagEntity> pendingTags = [];

    /// <inheritdoc/>
    public void AcceptPendingTags()
    {
        foreach (var (wallhavenTagId, tag) in pendingTags) resolvedTags[wallhavenTagId] = tag;

        pendingTags.Clear();
    }

    /// <inheritdoc/>
    public void DiscardPendingTags() => pendingTags.Clear();

    /// <inheritdoc/>
    public Task<Exceptional<Unit>> LinkTagsAsync(FileId fileId, IReadOnlyList<WallpaperTag> tags, CancellationToken cancellationToken)
        => Try.RunAsync(async () =>
        {
            var distinctTags = tags.DistinctBy(tag => tag.WallhavenTagId).ToList();
            await ResolveUncachedTagsAsync(distinctTags, cancellationToken);

            foreach (var tag in distinctTags)
            {
                fileTagRepository.Add(new FileTagEntity { FileId = fileId, TagId = KnownTag(tag.WallhavenTagId).Id })
                    .Match(_ => Unit.Instance, ex => throw ex);
            }

            return Unit.Instance;
        });

    private async Task ResolveUncachedTagsAsync(List<WallpaperTag> distinctTags, CancellationToken cancellationToken)
    {
        var uncachedTags = distinctTags.Where(tag => !IsKnown(tag.WallhavenTagId)).ToList();
        if (uncachedTags.Count == 0) return;

        var existingTags = (await tagsQuery.FindByWallhavenIdsAsync([.. uncachedTags.Select(tag => tag.WallhavenTagId)], cancellationToken))
            .Match(found => found, exception => throw exception);
        foreach (var existing in existingTags) resolvedTags[existing.WallhavenTagId] = existing;

        var tagRepository = unitOfWork.GetRepository<TagEntity, TagId>();
        foreach (var tag in uncachedTags.Where(tag => !IsKnown(tag.WallhavenTagId)))
        {
            pendingTags[tag.WallhavenTagId] = tagRepository.Add(new TagEntity
            {
                Id = TagId.Empty,
                WallhavenTagId = tag.WallhavenTagId,
                Name = tag.Name,
                Alias = tag.Alias,
                CategoryId = tag.CategoryId,
                Category = tag.Category,
                Purity = tag.Purity
            }).Match(added => added, ex => throw ex);
        }
    }

    private bool IsKnown(int wallhavenTagId) => resolvedTags.ContainsKey(wallhavenTagId) || pendingTags.ContainsKey(wallhavenTagId);

    private TagEntity KnownTag(int wallhavenTagId) => resolvedTags.TryGetValue(wallhavenTagId, out var tag) ? tag : pendingTags[wallhavenTagId];
}
