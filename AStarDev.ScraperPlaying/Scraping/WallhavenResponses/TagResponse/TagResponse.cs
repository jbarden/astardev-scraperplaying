using System.Text.Json.Serialization;

namespace AStarDev.ScraperPlaying.Scraping.WallhavenResponses.TagResponse;

public record TagResponse([property: JsonPropertyName("data")] Data Data);