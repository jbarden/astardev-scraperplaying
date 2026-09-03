using System.Text.Json.Serialization;

namespace AStarDev.ScraperPlaying.SearchAPI.DetailResponse;

public record Avatar(
    [property: JsonPropertyName("_00px")]
    string P00px,
    [property: JsonPropertyName("_28px")]
    string P28px,
    [property: JsonPropertyName("_2px")]
    string P2px,
    [property: JsonPropertyName("_0px")]
    string P0px
);