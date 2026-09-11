using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.ControlDb.TagDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.SearchAPI.DetailResponse;

namespace AStarDev.ScraperPlaying.Home;

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

    /// <inheritdoc/>
    public Task<Exceptional<UnitFp>> FetchAndLinkTagsAsync(string wallpaperId, FileId fileId, HttpClient client, IProgress<string> progress, CancellationToken cancellationToken)
        => Try.RunAsync(async () =>
        {
            progress.Report($"Fetching tags for wallpaper {wallpaperId}.");

            var detailResponse = (await jsonResponseProcessor.GetFromJsonAsync<DetailResponse>($"{ApplicationConstants.WallhavenDetailPathTemplate}{wallpaperId}", client, cancellationToken))
                .Match(
                    option => option.Match(value => value, () => throw new InvalidOperationException($"No response body received for wallpaper {wallpaperId} detail.")),
                    exception => throw exception);

            var tagRepository = unitOfWork.GetRepository<TagEntity, TagId>();
            var linkedTagIds = new HashSet<int>();

            foreach (var tag in detailResponse.Data.Tags)
            {
                if (!linkedTagIds.Add(tag.Id)) continue;

                if (!resolvedTags.TryGetValue(tag.Id, out var tagEntity))
                {
                    tagEntity = (await tagsQuery.TryFindByWallhavenIdAsync(tag.Id, cancellationToken))
                        .Match(
                            option => option.Match(
                                existing => existing,
                                () => tagRepository.Add(new TagEntity
                                {
                                    Id = TagId.Empty,
                                    WallhavenTagId = tag.Id,
                                    Name = tag.Name,
                                    Alias = tag.Alias,
                                    CategoryId = tag.CategoryId,
                                    Category = tag.Category,
                                    Purity = tag.Purity
                                }).Match(added => added, ex => throw ex)),
                            exception => throw exception);

                    resolvedTags.Add(tag.Id, tagEntity);
                }

                fileTagRepository.Add(new FileTagEntity { FileId = fileId, TagId = tagEntity.Id })
                    .Match(_ => UnitFp.Instance, ex => throw ex);
            }

            return UnitFp.Instance;
        });
}
