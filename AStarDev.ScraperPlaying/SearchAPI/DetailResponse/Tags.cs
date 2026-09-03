using System.Text.Json.Serialization;

namespace AStarDev.ScraperPlaying.SearchAPI.DetailResponse;

public record Tags(
    int Id,
    string Name,
    string Alias,
    [property: JsonPropertyName("category_id")] int CategoryId,
    string Category,
    string Purity,
    [property: JsonPropertyName("created_at")] string CreatedAt
);