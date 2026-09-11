using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.ControlDb.TagDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.SearchAPI.DetailResponse;

namespace AStarDev.ScraperPlaying.Home;

/// <inheritdoc/>
public class TagsProcessor(IJsonResponseProcessor jsonResponseProcessor, ITagsQuery tagsQuery, IUnitOfWork unitOfWork, IFileTagRepository fileTagRepository) : ITagsProcessor
{
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
            var resolvedTags = new Dictionary<int, TagEntity>();

            foreach (var tag in detailResponse.Data.Tags)
            {
                if (resolvedTags.ContainsKey(tag.Id)) continue;

                var tagEntity = (await tagsQuery.TryFindByWallhavenIdAsync(tag.Id, cancellationToken))
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

                fileTagRepository.Add(new FileTagEntity { FileId = fileId, TagId = tagEntity.Id })
                    .Match(_ => UnitFp.Instance, ex => throw ex);
            }

            return UnitFp.Instance;
        });
}
