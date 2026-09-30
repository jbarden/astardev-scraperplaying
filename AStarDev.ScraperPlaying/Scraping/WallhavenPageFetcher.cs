using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;
using Microsoft.Extensions.Logging;

namespace AStarDev.ScraperPlaying.Scraping;

/// <inheritdoc/>
public sealed class WallhavenPageFetcher(IJsonResponseProcessor jsonResponseProcessor, ILogger<WallhavenPageFetcher> logger) : IWallhavenPageFetcher
{
    /// <inheritdoc/>
    public async Task<SearchResponse> FetchPageAsync(string logLabel, Uri pageUrl, int page, HttpClient client, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var pageName = $"{logLabel} page {page}";
        progress.Report($"Fetching {pageName}.");
        LogMessage.PageView(logger, pageName);

        return (await jsonResponseProcessor.GetFromJsonAsync<SearchResponse>(pageUrl, client, cancellationToken))
            .Match(
                option => option.Match(value => value, () => throw new InvalidOperationException($"No response body received for {pageUrl}.")),
                exception => throw exception);
    }
}
