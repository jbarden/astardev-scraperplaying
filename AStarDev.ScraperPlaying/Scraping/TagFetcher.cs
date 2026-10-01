using AStarDev.ControlDb;
using AStarDev.ControlDb.TagDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.DetailResponse;
using AStarDev.ScraperPlaying.Tags;

namespace AStarDev.ScraperPlaying.Scraping;

/// <inheritdoc/>
public sealed class TagFetcher(IJsonResponseProcessor jsonResponseProcessor, ITagsQuery tagsQuery, TagFlagStore flagStore) : ITagFetcher
{
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
            var flagCache = flagStore.Cache;

            return [.. detailResponse.Data.Tags.Select(tag => tag with { IgnoreImage = flagCache.IsIgnored(tag.Id), IsName = flagCache.IsName(tag.Id), IsFamous = IsFamous(flagCache, tag, personCategories) })];
        });

    private async Task LoadTagFlagsAsync(CancellationToken cancellationToken)
    {
        if (flagStore.IsLoaded) return;

        flagStore.Load(TagFlagCache.From((await tagsQuery.GetFlagsAsync(cancellationToken)).Match(found => found, exception => throw exception)));
    }

    private static bool IsFamous(TagFlagCache flagCache, Tag tag, IReadOnlyList<string> personCategories)
        => flagCache.IsStored(tag.Id) ? flagCache.IsFamous(tag.Id) : FamousTagCheck.IsFamous(tag, personCategories);
}
