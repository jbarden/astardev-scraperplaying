using System.Text.Json;
using AStarDev.ScraperPlaying.SearchAPI.SearchResponse;
using Shouldly;
using Xunit;

namespace AStarDev.ScraperPlaying.TestsUnit;

public sealed class GivenAResponseModel
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void when_search_json_is_deserialized_then_wire_names_are_mapped()
    {
        const string json = """
            {
              "data": [{
                "id": "abc",
                "url": "https://example.test/image",
                "short_url": "abc",
                "views": 12,
                "favorites": 3,
                "source": "",
                "purity": "sfw",
                "category": "general",
                "dimension_x": 1920,
                "dimension_y": 1080,
                "resolution": "1920x1080",
                "ratio": "16x9",
                "file_size": 100,
                "file_type": "image/jpeg",
                "created_at": "2024-01-01",
                "colors": [],
                "path": "https://example.test/image.jpg",
                "thumbs": { "large": "large", "original": "original", "small": "small" }
              }],
              "meta": { "current_page": 1, "last_page": 1, "per_page": "24", "total": 1, "query": "test", "seed": null }
            }
            """;

        var response = JsonSerializer.Deserialize<SearchResponse>(json, JsonOptions)!;

        response.Data.Single().ShortUrl.ShouldBe("abc");
        response.Data.Single().DimensionX.ShouldBe(1920);
        response.Data.Single().Thumbs.large.ShouldBe("large");
        response.Meta.CurrentPage.ShouldBe(1);
    }
}