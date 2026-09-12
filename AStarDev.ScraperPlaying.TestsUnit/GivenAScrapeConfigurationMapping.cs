using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.ScraperPlaying.SearchAPI;

namespace AStarDev.ScraperPlaying.TestsUnit;

public sealed class GivenAScrapeConfigurationMapping
{
    [Fact]
    public void when_an_entity_is_mapped_then_urls_are_built_from_the_search_configuration()
    {
        var entity = new ScrapeConfigurationEntity(Guid.Empty)
        {
            SearchConfiguration = new SearchConfigurationEntity(Guid.Empty, Guid.Empty, "search term", 10, [])
            {
                BaseUrl = new Uri("https://example.test/"),
                SearchStringPrefix = "/search?q=id:",
                SearchStringSuffix = "&page=1",
                TopWallpapers = "top",
                Subscriptions = "subscriptions"
            },
            UserConfiguration = new UserConfigurationEntity(Guid.Empty, Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty)
        };

        var dto = entity.ToDto();

        dto.SearchCategoriesUrl.ShouldBe(new Uri("https://example.test//search?q=id:{id}&page=1"));
        dto.TopWallpapersUrl.ShouldBe(new Uri("https://example.test/top"));
        dto.SubscribedUrl.ShouldBe(new Uri("https://example.test/subscriptions"));
    }

    [Fact]
    public void when_an_entity_has_search_categories_then_they_are_mapped_onto_the_dto()
    {
        var entity = new ScrapeConfigurationEntity(Guid.Empty)
        {
            SearchConfiguration = new SearchConfigurationEntity(Guid.Empty, Guid.Empty, "search term", 10,
                [new() { Id = "1", Name = "General" }, new() { Id = "2", Name = "Anime" }])
            {
                BaseUrl = new Uri("https://example.test/")
            },
            UserConfiguration = new UserConfigurationEntity(Guid.Empty, Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty)
        };

        var dto = entity.ToDto();

        dto.SearchCategories.Length.ShouldBe(2);
        dto.SearchCategories[0].Id.ShouldBe("1");
        dto.SearchCategories[1].Id.ShouldBe("2");
    }

    [Fact]
    public void when_an_entity_has_no_search_categories_then_the_dto_has_an_empty_array()
    {
        var entity = new ScrapeConfigurationEntity(Guid.Empty)
        {
            SearchConfiguration = new SearchConfigurationEntity(Guid.Empty, Guid.Empty, "search term", 10, [])
            {
                BaseUrl = new Uri("https://example.test/")
            },
            UserConfiguration = new UserConfigurationEntity(Guid.Empty, Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty)
        };

        var dto = entity.ToDto();

        dto.SearchCategories.ShouldBeEmpty();
    }
}
