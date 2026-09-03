using System.Text.Json.Serialization;

namespace AStarDev.ScraperPlaying.SearchAPI;

public record Data(
    string Id,
    string Url,
    [property: JsonPropertyName("short_url")]
    string ShortUrl,
    int Views,
    int Favorites,
    string Source,
    string Purity,
    string Category,
    [property: JsonPropertyName("dimension_x")]
    int DimensionX,
    [property: JsonPropertyName("dimension_y")]
    int DimensionY,
    string Resolution,
    string Ratio,
    [property: JsonPropertyName("file_size")]
    int FileSize,
    [property: JsonPropertyName("file_type")]
    string FileType,
    [property: JsonPropertyName("created_at")]
    string CreatedAt,
    string[] Colors,
    string Path,
    Thumbs Thumbs
);