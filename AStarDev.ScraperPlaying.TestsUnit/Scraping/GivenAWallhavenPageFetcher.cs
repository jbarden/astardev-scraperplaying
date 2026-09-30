using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;

namespace AStarDev.ScraperPlaying.TestsUnit.Scraping;

public sealed class GivenAWallhavenPageFetcher
{
    private static readonly Uri PageUrl = new("https://example.test/page/2");
    private readonly FakeJsonResponseProcessor jsonResponseProcessor = new();
    private readonly CapturingProgress progress = new();
    private readonly WallhavenPageFetcher fetcher;

    public GivenAWallhavenPageFetcher() => fetcher = new(jsonResponseProcessor);

    [Fact]
    public async Task when_a_page_is_fetched_then_the_response_is_returned_and_the_fetch_is_reported()
    {
        var response = new SearchResponse([], new Meta(5));
        jsonResponseProcessor.Response = (Option<SearchResponse>)response;

        var fetched = await Fetch();

        (fetched, jsonResponseProcessor.RequestedUrls.Single(), progress.Messages.Single()).ShouldBe((response, PageUrl, "Fetching top wallpapers page 2."));
    }

    [Fact]
    public async Task when_the_response_has_no_body_then_it_throws_naming_the_url()
    {
        jsonResponseProcessor.Response = Option<SearchResponse>.None.Instance;

        var thrown = await Should.ThrowAsync<InvalidOperationException>(Fetch);

        thrown.Message.ShouldBe("No response body received for https://example.test/page/2.");
    }

    [Fact]
    public async Task when_the_fetch_fails_then_the_failure_is_rethrown()
    {
        var failure = new InvalidOperationException("page fetch failed");
        jsonResponseProcessor.Response = failure;

        var thrown = await Should.ThrowAsync<InvalidOperationException>(Fetch);

        thrown.ShouldBeSameAs(failure);
    }

    private async Task<SearchResponse> Fetch()
    {
        using var client = new HttpClient();

        return await fetcher.FetchPageAsync("top wallpapers", PageUrl, 2, client, progress, CancellationToken.None);
    }

    private sealed class FakeJsonResponseProcessor : IJsonResponseProcessor
    {
        public List<Uri> RequestedUrls { get; } = [];

        public Exceptional<Option<SearchResponse>> Response { get; set; } = Option<SearchResponse>.None.Instance;

        public Task<Exceptional<Option<T>>> GetFromJsonAsync<T>(Uri url, HttpClient client, CancellationToken cancellationToken)
        {
            RequestedUrls.Add(url);

            return Task.FromResult((Exceptional<Option<T>>)(object)Response);
        }
    }

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }
}
