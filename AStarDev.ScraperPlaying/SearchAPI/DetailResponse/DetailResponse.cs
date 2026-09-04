using System.Text.Json.Serialization;

namespace AStarDev.ScraperPlaying.SearchAPI.DetailResponse;

public record DetailResponse(
    [property: JsonPropertyName("data")]
    Data Data
);