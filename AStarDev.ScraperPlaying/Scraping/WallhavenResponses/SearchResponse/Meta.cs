using System.Text.Json.Serialization;

namespace AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;

public record Meta([property: JsonPropertyName("last_page")] int LastPage);
