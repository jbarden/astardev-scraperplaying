using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;

namespace AStarDev.ScraperPlaying.Scraping;

/// <inheritdoc/>
public sealed class WallhavenPageFetcher(IJsonResponseProcessor jsonResponseProcessor) : IWallhavenPageFetcher
{
    /// <inheritdoc/>
    public async Task<SearchResponse> FetchPageAsync(string logLabel, Uri pageUrl, int page, HttpClient client, IProgress<string> progress, CancellationToken cancellationToken)
    {
        progress.Report($"Fetching {logLabel} page {page}.");

        return (await jsonResponseProcessor.GetFromJsonAsync<SearchResponse>(pageUrl, client, cancellationToken))
            .Match(
                option => option.Match(value => value, () => throw new InvalidOperationException($"No response body received for {pageUrl}.")),
                exception => throw exception);
    }
}
