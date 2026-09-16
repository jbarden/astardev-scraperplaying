using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.ScraperPlaying.Scraping;

namespace AStarDev.ScraperPlaying.TestsUnit.Scraping;

public sealed class GivenAWallhavenUrlBuilder
{
    [Fact]
    public void when_building_a_top_wallpapers_page_url_then_the_page_number_is_appended()
        => WallhavenUrlBuilder.BuildTopWallpapersPageUrl("https://example.test/top/", 3).ShouldBe("https://example.test/top/3");

    [Fact]
    public void when_building_a_category_page_url_for_page_one_then_only_the_id_placeholder_is_substituted()
    {
        var category = new SearchCategoryEntity { SearchConfigurationId = new SearchConfigurationId(Guid.CreateVersion7()), Id = "cat1", Name = "category one" };

        WallhavenUrlBuilder.BuildCategoryPageUrl("https://example.test/search?id=", category, 1)
            .ShouldBe("https://example.test/search?q=id:cat1&page=1");
    }

    [Fact]
    public void when_building_a_category_page_url_for_a_later_page_then_the_page_number_is_appended_after_the_substituted_id()
    {
        var category = new SearchCategoryEntity { SearchConfigurationId = new SearchConfigurationId(Guid.CreateVersion7()), Id = "cat1", Name = "category one" };

        WallhavenUrlBuilder.BuildCategoryPageUrl("https://example.test/search?id=", category, 2)
            .ShouldBe("https://example.test/search?q=id:cat1&page=2");
    }
}
//https://wallhaven.cc/search?q=id:10193