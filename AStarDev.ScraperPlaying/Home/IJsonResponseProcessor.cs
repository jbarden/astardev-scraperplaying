namespace AStarDev.ScraperPlaying.Home;

/// <summary>
/// Interface for processing JSON responses by fetching and deserializing them asynchronously.  
/// </summary>
public interface IJsonResponseProcessor
{
    /// <summary>
    /// Fetches a JSON response from the specified URL and deserializes it into the specified type asynchronously.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the JSON response into.</typeparam>
    /// <param name="url">The URL to fetch the JSON response from.</param>
    /// <param name="client">The HTTP client used to make the request.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation, containing the deserialized object, or null only if the response was successful but its body deserializes to null.</returns>
    /// <exception cref="HttpRequestException">The response status code does not indicate success.</exception>
    /// <exception cref="InvalidOperationException">The successful response body could not be deserialized as <typeparamref name="T"/>.</exception>
    Task<T?> GetFromJsonAsync<T>(string url, HttpClient client, CancellationToken cancellationToken);
}