using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.DetailResponse;
using AStarDev.ScraperPlaying.WallpaperIngestion;

namespace AStarDev.ScraperPlaying.TestsUnit.WallpaperIngestion;

public sealed class GivenAWallpaperFileNamer
{
    [Fact]
    public void when_the_wallpaper_has_no_person_name_tag_then_the_wallpaper_id_is_the_file_name()
        => WallpaperFileNamer.Create("abc123", ".jpg", [Tag("anime"), Tag("actress", "Celebrities"), Tag("men", "People")]).Value.ShouldBe("abc123.jpg");

    [Fact]
    public void when_the_wallpaper_has_no_tags_then_the_wallpaper_id_is_the_file_name()
        => WallpaperFileNamer.Create("abc123", ".png", []).Value.ShouldBe("abc123.png");

    [Theory]
    [InlineData("Celebrities")]
    [InlineData("Models")]
    [InlineData("Pornstars")]
    [InlineData("Other Figures")]
    [InlineData("Actress")]
    [InlineData("other figures")]
    public void when_the_wallpaper_has_a_person_name_tag_then_its_name_prefixes_the_file_name(string category)
        => WallpaperFileNamer.Create("abc123", ".jpg", [Tag("Max Verstappen", category), Tag("men", "People")]).Value.ShouldBe("Max_Verstappen_abc123.jpg");

    [Fact]
    public void when_a_non_name_tag_shares_a_person_category_then_only_the_name_is_used()
        => WallpaperFileNamer.Create("abc123", ".jpg", [Tag("finger pointing", "Other Figures"), Tag("Max Verstappen", "Other Figures")]).Value.ShouldBe("Max_Verstappen_abc123.jpg");

    [Fact]
    public void when_a_tag_is_in_the_photographers_category_then_it_is_not_used()
        => WallpaperFileNamer.Create("abc123", ".jpg", [Tag("Ansel Adams", "Photographers"), Tag("Emma Watson", "Celebrities")]).Value.ShouldBe("Emma_Watson_abc123.jpg");

    [Fact]
    public void when_a_capitalised_tag_is_outside_the_person_categories_then_it_is_not_used()
        => WallpaperFileNamer.Create("abc123", ".jpg", [Tag("Formula 1", "Sports"), Tag("Max Verstappen", "")]).Value.ShouldBe("abc123.jpg");

    [Fact]
    public void when_the_wallpaper_has_several_person_name_tags_then_they_are_all_joined_in_page_order()
        => WallpaperFileNamer.Create("abc123", ".jpg", [Tag("Emma Watson", "Celebrities"), Tag("brunette"), Tag("Daniel Radcliffe", "Celebrities")]).Value.ShouldBe("Emma_Watson_Daniel_Radcliffe_abc123.jpg");

    [Fact]
    public void when_a_person_name_has_invalid_file_name_characters_then_they_are_removed()
        => WallpaperFileNamer.Create("abc123", ".jpg", [Tag("AC/DC: \"Live\"?", "Celebrities")]).Value.ShouldBe("ACDC_Live_abc123.jpg");

    [Fact]
    public void when_a_person_name_is_only_invalid_file_name_characters_then_it_is_skipped()
        => WallpaperFileNamer.Create("abc123", ".jpg", [Tag("???", "Celebrities")]).Value.ShouldBe("abc123.jpg");

    [Fact]
    public void when_the_prefix_is_very_long_then_it_is_truncated_so_the_file_name_stays_within_limits()
        => WallpaperFileNamer.Create("abc123", ".jpg", [Tag($"A{new string('a', 400)}", "Celebrities")]).Value.ShouldBe($"A{new string('a', 99)}_abc123.jpg");

    private static Tag Tag(string name, string category = "") => new(1, name, name, 1, category, "sfw");
}
