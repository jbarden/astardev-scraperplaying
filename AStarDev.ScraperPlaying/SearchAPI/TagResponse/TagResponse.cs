using System.Text.Json.Serialization;

namespace AStarDev.ScraperPlaying.SearchAPI.TagResponse;

public record TagResponse([property: JsonPropertyName("data")] Data Data);