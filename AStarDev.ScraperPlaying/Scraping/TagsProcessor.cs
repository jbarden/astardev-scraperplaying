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
    /// <see cref="ITagsQuery.TryFindByWallhavenIdAsync"/>'s database fast-path (the first one's insert isn't
    /// saved yet) and both try to add a duplicate <see cref="TagEntity"/>, failing the whole page's save on the
    /// unique-index violation.
    /// </summary>
    private readonly Dictionary<int, TagEntity> resolvedTags = [];

    /// <inheritdoc/>
    public Task<Exceptional<Unit>> LinkTagsAsync(FileId fileId, IReadOnlyList<WallpaperTag> tags, CancellationToken cancellationToken)
        => Try.RunAsync(async () =>
        {
            var tagRepository = unitOfWork.GetRepository<TagEntity, TagId>();
            var linkedTagIds = new HashSet<int>();

            foreach (var tag in tags)
            {
                if (!linkedTagIds.Add(tag.WallhavenTagId)) continue;

                if (!resolvedTags.TryGetValue(tag.WallhavenTagId, out var tagEntity))
                {
                    tagEntity = (await tagsQuery.TryFindByWallhavenIdAsync(tag.WallhavenTagId, cancellationToken))
                        .Match(
                            option => option.Match(
                                existing => existing,
                                () => tagRepository.Add(new TagEntity
                                {
                                    Id = TagId.Empty,
                                    WallhavenTagId = tag.WallhavenTagId,
                                    Name = tag.Name,
                                    Alias = tag.Alias,
                                    CategoryId = tag.CategoryId,
                                    Category = tag.Category,
                                    Purity = tag.Purity
                                }).Match(added => added, ex => throw ex)),
                            exception => throw exception);

                    resolvedTags.Add(tag.WallhavenTagId, tagEntity);
                }

                fileTagRepository.Add(new FileTagEntity { FileId = fileId, TagId = tagEntity.Id })
                    .Match(_ => Unit.Instance, ex => throw ex);
            }

            return Unit.Instance;
        });
}
