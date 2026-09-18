using AStarDev.ScraperPlaying.Scraping;

namespace AStarDev.ScraperPlaying.TestsUnit.Scraping;

public sealed class GivenFamousTags
{
    [Theory]
    [InlineData("actress")]
    [InlineData("Model")]
    [InlineData("female singer")]
    [InlineData("SINGER-songwriter")]
    [InlineData("supermodels")]
    public void when_a_tag_contains_a_famous_fragment_then_the_tags_are_famous(string tagName)
        => FamousTags.AreFamous([new WallpaperTag(1, "anime"), new WallpaperTag(2, tagName)]).ShouldBeTrue();

    [Fact]
    public void when_no_tag_contains_a_famous_fragment_then_the_tags_are_not_famous()
        => FamousTags.AreFamous([new WallpaperTag(1, "anime"), new WallpaperTag(2, "landscape")]).ShouldBeFalse();

    [Fact]
    public void when_there_are_no_tags_then_the_tags_are_not_famous()
        => FamousTags.AreFamous([]).ShouldBeFalse();
}
