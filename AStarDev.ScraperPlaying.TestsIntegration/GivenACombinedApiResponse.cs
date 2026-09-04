using System.Text.Json;
using AStarDev.ScraperPlaying.SearchAPI.DetailResponse;
using Shouldly;
using Xunit;

namespace AStarDev.ScraperPlaying.TestsIntegration;

public sealed class GivenACombinedApiResponse
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void when_detail_json_is_deserialized_then_nested_api_data_is_preserved()
    {
        const string json = """
            {
              "data": {
                "id": "abc",
                "url": "https://example.test/image",
                "short_url": "abc",
                "uploader": { "username": "artist", "group": "group", "avatar": { "_00px": "avatar" } },
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
                "thumbs": { "large": "large", "original": "original", "small": "small" },
                "tags": []
              }
            }
            """;

        var response = JsonSerializer.Deserialize<DetailResponse>(json, JsonOptions)!;

        response.Data.Uploader.Username.ShouldBe("artist");
        response.Data.Thumbs.Original.ShouldBe("original");
    }
}