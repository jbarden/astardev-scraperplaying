using System.Net.Http.Json;
using System.Text.Json;

namespace AStarDev.ScraperPlaying.Home;

/// <inheritdoc/>
public class JsonResponseProcessor : IJsonResponseProcessor
{
    /// <inheritdoc/>
    public async Task<T?> GetFromJsonAsync<T>(string url, HttpClient client, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Wallhaven returned {(int)response.StatusCode} ({response.StatusCode}) for {url}. " +
                $"Location: {response.Headers.Location}. Response: {responseBody}");
        }

        try
        {

            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            throw new InvalidOperationException($"Unable to deserialize response from {url} as {typeof(T).Name}.", exception);
        }
    }
}
