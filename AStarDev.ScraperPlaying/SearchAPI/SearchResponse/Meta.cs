using System.Text.Json.Serialization;

namespace AStarDev.ScraperPlaying.SearchAPI;

public record Meta(
    [property: JsonPropertyName("current_page")]
    int CurrentPage,
    [property: JsonPropertyName("last_page")]
    int LastPage,
    [property: JsonPropertyName("per_page")]    
    string PerPage,
    int Total,
    string Query,
    object Seed
);