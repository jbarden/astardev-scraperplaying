using System.Net;
using System.Text;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Home;

namespace AStarDev.ScraperPlaying.TestsUnit;

public sealed class GivenAJsonResponseProcessor
{
    private readonly JsonResponseProcessor processor = new();

    [Fact]
    public async Task when_the_response_is_successful_and_the_body_deserializes_then_a_success_is_returned()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"name":"cats"}""", Encoding.UTF8, "application/json")
        });

        var result = await processor.GetFromJsonAsync<TestPayload>("https://example.test/search", client, CancellationToken.None);

        result.Match(value => value?.Name, ex => ex.Message).ShouldBe("cats");
    }

    [Fact]
    public async Task when_the_response_status_is_not_successful_then_a_failure_is_returned_without_throwing()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("not found")
        });

        var result = await processor.GetFromJsonAsync<TestPayload>("https://example.test/search", client, CancellationToken.None);

        var capturedException = result.Match(_ => (Exception?)null, ex => ex);
        capturedException.ShouldBeOfType<HttpRequestException>();
        capturedException!.Message.ShouldContain("404");
        capturedException.Message.ShouldContain("https://example.test/search");
    }

    [Fact]
    public async Task when_the_response_body_cannot_be_deserialized_then_a_failure_is_returned_without_throwing()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not json", Encoding.UTF8, "application/json")
        });

        var result = await processor.GetFromJsonAsync<TestPayload>("https://example.test/search", client, CancellationToken.None);

        var capturedException = result.Match(_ => (Exception?)null, ex => ex);
        capturedException.ShouldBeOfType<InvalidOperationException>();
        capturedException!.Message.ShouldContain(nameof(TestPayload));
    }

    [Fact]
    public async Task when_the_cancellation_token_is_already_cancelled_then_the_operation_is_cancelled_not_captured()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK));

        await Should.ThrowAsync<OperationCanceledException>(
            () => processor.GetFromJsonAsync<TestPayload>("https://example.test/search", client, cancellationTokenSource.Token));
    }

    private static HttpClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> responder)
        => new(new StubHttpMessageHandler(responder));

    private sealed record TestPayload(string Name);

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }
}
