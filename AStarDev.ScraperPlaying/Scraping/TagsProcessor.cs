using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.ControlDb.TagDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Tags;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.DetailResponse;

namespace AStarDev.ScraperPlaying.Scraping;

/// <inheritdoc/>
public class TagsProcessor(IJsonResponseProcessor jsonResponseProcessor, ITagsQuery tagsQuery, IUnitOfWork unitOfWork, IFileTagRepository fileTagRepository) : ITagsProcessor
{
    /// <summary>
    /// Caches resolved tags for the lifetime of this instance (one scrape run - <see cref="TagsProcessor"/> is
    /// Scoped). <see cref="PagesProcessor"/> batches <c>SaveChangesAsync</c> once per page, so without this,
    /// two different wallpapers on the same page introducing the same new tag would both miss
    /// <see cref="ITagsQuery.TryFindByWallhavenIdAsync"/>'s database fast-path (the first one's insert isn't
    /// saved yet) and both try to add a duplicate <see cref="TagEntity"/>, failing the whole page's save on the
    /// unique-index violation.
    /// </summary>
    private readonly Dictionary<int, TagEntity> resolvedTags = [];

    /// <summary>
    /// The flags of every tag stored when the run started, loaded with a single query on the first fetch and reused for the rest of the run: the flags do not change during a scrape.
    /// The first fetch always completes before any other database work starts, so the load never overlaps another use of the database context.
    /// </summary>
    private TagFlagCache flagCache = TagFlagCache.Empty;

    private bool flagCacheLoaded;

    /// <inheritdoc/>
    public Task<Exceptional<IReadOnlyList<Tag>>> FetchTagsAsync(string wallpaperId, HttpClient client, IReadOnlyList<string> personCategories, IProgress<string> progress, CancellationToken cancellationToken)
        => Try.RunAsync<IReadOnlyList<Tag>>(async () =>
        {
            progress.Report($"Fetching tags for wallpaper {wallpaperId}.");

            var detailResponse = (await jsonResponseProcessor.GetFromJsonAsync<DetailResponse>(new Uri($"{ApplicationConstants.WallhavenDetailPathTemplate}{wallpaperId}", UriKind.Relative), client, cancellationToken))
                .Match(
                    option => option.Match(value => value, () => throw new InvalidOperationException($"No response body received for wallpaper {wallpaperId} detail.")),
                    exception => throw exception);

            await LoadTagFlagsAsync(cancellationToken);

            return [.. detailResponse.Data.Tags.Select(tag => tag with { IgnoreImage = flagCache.IsIgnored(tag.Id), IsName = flagCache.IsName(tag.Id), IsFamous = IsFamous(tag, personCategories) })];
        });

    private async Task LoadTagFlagsAsync(CancellationToken cancellationToken)
    {
        if (flagCacheLoaded) return;

        flagCache = TagFlagCache.From((await tagsQuery.GetFlagsAsync(cancellationToken)).Match(found => found, exception => throw exception));
        flagCacheLoaded = true;
    }

    private bool IsFamous(Tag tag, IReadOnlyList<string> personCategories)
        => flagCache.IsStored(tag.Id) ? flagCache.IsFamous(tag.Id) : FamousTagCheck.IsFamous(tag, personCategories);

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
        IReadOnlyList<int> uncachedIds = [.. tags.Where(tag => !resolvedTags.ContainsKey(tag.Id) && (!flagCacheLoaded || flagCache.IsStored(tag.Id))).Select(tag => tag.Id)];
        if (uncachedIds.Count == 0) return;

        var existingTags = (await tagsQuery.FindByWallhavenIdsAsync(uncachedIds, cancellationToken))
            .Match(found => found, exception => throw exception);

        foreach (var existingTag in existingTags)
        {
            resolvedTags[existingTag.WallhavenTagId] = existingTag;
        }
    }
}
