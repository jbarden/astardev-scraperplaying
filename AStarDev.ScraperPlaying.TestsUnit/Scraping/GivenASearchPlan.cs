using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;

namespace AStarDev.ScraperPlaying.TestsUnit.Scraping;

public sealed class GivenASearchPlan
{
    [Fact]
    public void when_the_configuration_has_categories_then_each_is_planned_before_top_wallpapers()
    {
        var plan = SearchPlan.Build(CreateConfiguration("cat1", "cat2"));

        plan.Select(search => search.LogLabel).ShouldBe(["search category cat1", "search category cat2", "top wallpapers"]);
    }

    [Fact]
    public void when_a_category_search_is_planned_then_it_carries_the_category_name_and_builds_page_urls()
    {
        var search = SearchPlan.Build(CreateConfiguration("cat1"))[0];

        search.CategoryName.Match(name => name, () => string.Empty).ShouldBe("name-cat1");
        search.PageUrlFactory(3).ShouldBe("search/cat1suffix3");
    }

    [Fact]
    public void when_top_wallpapers_is_planned_then_it_has_no_category_and_builds_page_urls()
    {
        var search = SearchPlan.Build(CreateConfiguration())[0];

        search.LogLabel.ShouldBe("top wallpapers");
        search.CategoryName.Match(_ => false, () => true).ShouldBeTrue();
        search.PageUrlFactory(2).ShouldBe("top/2");
    }

    private static ScrapeConfigurationEntity CreateConfiguration(params string[] categoryIds)
    {
        var scrapeConfigurationId = new ScrapeConfigurationId(Guid.CreateVersion7());
        var searchConfigurationId = new SearchConfigurationId(Guid.CreateVersion7());
        var categories = categoryIds
            .Select(id => new SearchCategoryEntity { SearchConfigurationId = searchConfigurationId, Id = id, Name = $"name-{id}" })
            .ToList();

        return new ScrapeConfigurationEntity(scrapeConfigurationId)
        {
            BaseUrl = new Uri("https://example.test"),
            TopWallpapers = "top/",
            SearchStringPrefix = "search/",
            SearchStringSuffix = "suffix",
            UserConfiguration = new UserConfigurationEntity(new UserConfigurationId(Guid.CreateVersion7()), scrapeConfigurationId, "user@example.test", "user", "secret", "api-key"),
            SearchConfiguration = new SearchConfigurationEntity(searchConfigurationId, scrapeConfigurationId, "cats", 10, categories),
            ScrapeDirectories = new ScrapeDirectoriesEntity(new ScrapeDirectoriesId(Guid.CreateVersion7()), scrapeConfigurationId, "/root", "/famous", "sub")
        };
    }
}
