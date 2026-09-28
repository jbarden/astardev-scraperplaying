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

    [Theory]
    [InlineData("Celebrities")]
    [InlineData("Models")]
    [InlineData("Pornstars")]
    [InlineData("Other Figures")]
    [InlineData("Photographers")]
    [InlineData("other figures")]
    public void when_a_capitalised_tag_is_in_a_person_category_then_it_is_a_person_name(string category)
        => FamousTags.IsPersonName(new WallpaperTag(1, "Max Verstappen", Category: category)).ShouldBeTrue();

    [Theory]
    [InlineData("finger pointing", "Other Figures")]
    [InlineData("Formula 1", "Sports")]
    [InlineData("Max Verstappen", "")]
    [InlineData("Max Verstappen", "People")]
    [InlineData("", "Celebrities")]
    public void when_a_tag_is_lower_case_or_outside_a_person_category_then_it_is_not_a_person_name(string name, string category)
        => FamousTags.IsPersonName(new WallpaperTag(1, name, Category: category)).ShouldBeFalse();

    [Fact]
    public void when_a_tag_is_a_person_name_then_the_tags_are_famous()
        => FamousTags.AreFamous([new WallpaperTag(1, "motorsport"), new WallpaperTag(2, "Max Verstappen", Category: "Other Figures")]).ShouldBeTrue();

    [Fact]
    public void when_only_a_non_name_tag_is_in_a_person_category_then_the_tags_are_not_famous()
        => FamousTags.AreFamous([new WallpaperTag(1, "finger pointing", Category: "Other Figures")]).ShouldBeFalse();
}
