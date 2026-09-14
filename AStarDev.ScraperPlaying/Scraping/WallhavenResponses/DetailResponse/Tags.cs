using System.Text.Json.Serialization;

namespace AStarDev.ScraperPlaying.Scraping.WallhavenResponses.DetailResponse;

public record Tag(
    int Id,
    string Name,
    string Alias,
    [property: JsonPropertyName("category_id")] int CategoryId,
    string Category,
    string Purity,
    [property: JsonPropertyName("created_at")] string CreatedAt
);