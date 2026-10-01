using AStarDev.ControlDb;
using AStarDev.FunctionalParadigm;
using Microsoft.Extensions.DependencyInjection;

namespace AStarDev.ScraperPlaying.Tags;

/// <inheritdoc/>
public sealed class TagCatalogue(IServiceScopeFactory scopeFactory) : ITagCatalogue
{
    /// <inheritdoc/>
    public async Task<Exceptional<IReadOnlyList<TagSummary>>> ListAsync(CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();

        return (await scope.ServiceProvider.GetRequiredService<ITagsQuery>().ListAsync(cancellationToken)).Match(
            tags => (Exceptional<IReadOnlyList<TagSummary>>)new Success<IReadOnlyList<TagSummary>>([.. tags.Select(TagSummary.From)]),
            exception => exception);
    }

    /// <inheritdoc/>
    public async Task<Exceptional<Unit>> SaveFlagsAsync(IReadOnlyDictionary<int, TagFlags> flagsByWallhavenId, CancellationToken cancellationToken = default)
    {
        if (flagsByWallhavenId.Count == 0) return Unit.Instance;

        using var scope = scopeFactory.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<ITagsQuery>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        return await Try.RunAsync(async () =>
        {
            var tags = (await query.FindByWallhavenIdsAsync([.. flagsByWallhavenId.Keys], cancellationToken)).Match(found => found, exception => throw exception);
            foreach (var tag in tags)
            {
                var flags = flagsByWallhavenId[tag.WallhavenTagId];
                tag.IgnoreImage = flags.IgnoreImage;
                tag.IsName = flags.IsName;
            }

            _ = await unitOfWork.SaveChangesAsync(cancellationToken);

            return Unit.Instance;
        });
    }
}
