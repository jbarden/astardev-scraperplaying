using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;

namespace AStarDev.ScraperPlaying.TestsUnit.Scraping;

public sealed class GivenASearchRequest
{
    [Fact]
    public void when_a_category_is_searched_then_the_label_is_the_category_name()
        => new SearchRequest("nature", Option.Some("Nature"), _ => string.Empty).CategoryLabel.ShouldBe("Nature");

    [Fact]
    public void when_the_top_wallpapers_are_searched_then_the_label_is_top_wallpapers()
        => new SearchRequest("top wallpapers", Option.None<string>(), _ => string.Empty).CategoryLabel.ShouldBe("Top Wallpapers");
}
