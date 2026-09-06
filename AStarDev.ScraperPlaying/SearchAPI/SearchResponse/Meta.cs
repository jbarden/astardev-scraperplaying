using System.Text.Json;
using System.Text.Json.Serialization;

namespace AStarDev.ScraperPlaying.SearchAPI.SearchResponse;

public record Meta(
    [property: JsonPropertyName("current_page")]
    int CurrentPage,
    [property: JsonPropertyName("last_page")]
    int LastPage,
    [property: JsonPropertyName("per_page")]
    int PerPage,
    int Total,
    JsonElement Query,
    object Seed
);