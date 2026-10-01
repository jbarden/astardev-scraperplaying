using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.ControlDb.TagDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.DetailResponse;

namespace AStarDev.ScraperPlaying.Scraping;

/// <inheritdoc/>
public sealed class TagLinker(ITagsQuery tagsQuery, IUnitOfWork unitOfWork, IFileTagRepository fileTagRepository, TagFlagStore flagStore) : ITagLinker
{
    /// <summary>
    /// Caches resolved tags for the lifetime of this instance (one scrape run - <see cref="TagLinker"/> is
    /// Scoped). <see cref="PagesProcessor"/> batches <c>SaveChangesAsync</c> once per page, so without this,
    /// two different wallpapers on the same page introducing the same new tag would both miss
    /// <see cref="ITagsQuery.TryFindByWallhavenIdAsync"/>'s database fast-path (the first one's insert isn't
    /// saved yet) and both try to add a duplicate <see cref="TagEntity"/>, failing the whole page's save on the
    /// unique-index violation.
    /// </summary>
    private readonly Dictionary<int, TagEntity> resolvedTags = [];

    /// <inheritdoc/>
    public Task<Exceptional<Unit>> LinkTagsAsync(FileId fileId, IReadOnlyList<Tag> tags, CancellationToken cancellationToken)
        => Try.RunAsync(async () =>
        {
            var distinctTags = tags.DistinctBy(tag => tag.Id).ToList();
            await CacheExistingTagsAsync(distinctTags, cancellationToken);

            foreach (var tag in distinctTags)
            {
                var tagEntity = ResolveOrAdd(tag);
                _ = fileTagRepository.Add(new FileTagEntity { FileId = fileId, TagId = tagEntity.Id })
                    .Match(_ => Unit.Instance, ex => throw ex);
            }

            return Unit.Instance;
        });

    private TagEntity ResolveOrAdd(Tag tag)
    {
        if (resolvedTags.TryGetValue(tag.Id, out var existing)) return existing;

        var added = unitOfWork.GetRepository<TagEntity, TagId>().Add(ToEntity(tag)).Match(entity => entity, ex => throw ex);
        resolvedTags.Add(tag.Id, added);

        return added;
    }

    private static TagEntity ToEntity(Tag tag) => new()
    {
        Id = TagId.Empty,
        WallhavenTagId = tag.Id,
        Name = tag.Name,
        Alias = tag.Alias,
        CategoryId = tag.CategoryId,
        Category = tag.Category,
        Purity = tag.Purity,
        IsFamous = tag.IsFamous
    };

    /// <summary>Loads, in one query, only the uncached tags that were stored when the run started. A tag that was not stored cannot be in the database, so it needs no lookup.</summary>
    private async Task CacheExistingTagsAsync(IReadOnlyList<Tag> tags, CancellationToken cancellationToken)
    {
        IReadOnlyList<int> uncachedIds = [.. tags.Where(tag => !resolvedTags.ContainsKey(tag.Id) && (!flagStore.IsLoaded || flagStore.Cache.IsStored(tag.Id))).Select(tag => tag.Id)];
        if (uncachedIds.Count == 0) return;

        var existingTags = (await tagsQuery.FindByWallhavenIdsAsync(uncachedIds, cancellationToken))
            .Match(found => found, exception => throw exception);

        foreach (var existingTag in existingTags)
        {
            resolvedTags[existingTag.WallhavenTagId] = existingTag;
        }
    }
}
