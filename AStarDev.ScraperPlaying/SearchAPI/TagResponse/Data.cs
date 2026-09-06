using System.Text.Json.Serialization;

namespace AStarDev.ScraperPlaying.SearchAPI.TagResponse;

public record Data(
    int Id,
    string Name,
    string Alias,
    [property: JsonPropertyName("category_id")] int CategoryId,
    string Category,
    string Purity,
    [property: JsonPropertyName("created_at")] string CreatedAt
);