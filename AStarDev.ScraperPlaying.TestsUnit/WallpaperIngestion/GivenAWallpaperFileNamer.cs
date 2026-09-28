using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.WallpaperIngestion;

namespace AStarDev.ScraperPlaying.TestsUnit.WallpaperIngestion;

public sealed class GivenAWallpaperFileNamer
{
    [Fact]
    public void when_the_wallpaper_has_no_person_name_tag_then_the_wallpaper_id_is_the_file_name()
        => WallpaperFileNamer.Create(CreateDetail(Tag("anime"), Tag("actress"), Tag("men", "People"))).Value.ShouldBe("abc123.jpg");

    [Fact]
    public void when_the_wallpaper_has_no_tags_then_the_wallpaper_id_is_the_file_name()
        => WallpaperFileNamer.Create(CreateDetail()).Value.ShouldBe("abc123.jpg");

    [Theory]
    [InlineData("Celebrities")]
    [InlineData("Models")]
    [InlineData("Pornstars")]
    [InlineData("Other Figures")]
    [InlineData("Photographers")]
    public void when_the_wallpaper_has_a_person_name_tag_then_its_name_prefixes_the_file_name(string category)
        => WallpaperFileNamer.Create(CreateDetail(Tag("Max Verstappen", category), Tag("men", "People"))).Value.ShouldBe("Max_Verstappen_abc123.jpg");

    [Fact]
    public void when_the_person_name_tag_shares_a_category_with_a_non_name_tag_then_only_the_name_is_used()
        => WallpaperFileNamer.Create(CreateDetail(Tag("finger pointing", "Other Figures"), Tag("Max Verstappen", "Other Figures"))).Value.ShouldBe("Max_Verstappen_abc123.jpg");

    [Fact]
    public void when_the_wallpaper_has_several_person_name_tags_then_they_are_all_joined_in_page_order()
        => WallpaperFileNamer.Create(CreateDetail(Tag("Emma Watson", "Celebrities"), Tag("brunette"), Tag("Daniel Radcliffe", "Celebrities"))).Value.ShouldBe("Emma_Watson_Daniel_Radcliffe_abc123.jpg");

    [Fact]
    public void when_a_person_name_has_invalid_file_name_characters_then_they_are_removed()
        => WallpaperFileNamer.Create(CreateDetail(Tag("AC/DC: \"Live\"?", "Celebrities"))).Value.ShouldBe("ACDC_Live_abc123.jpg");

    [Fact]
    public void when_a_person_name_is_only_invalid_file_name_characters_then_it_is_skipped()
        => WallpaperFileNamer.Create(CreateDetail(Tag("???", "Celebrities"))).Value.ShouldBe("abc123.jpg");

    [Fact]
    public void when_the_prefix_is_very_long_then_it_is_truncated_so_the_file_name_stays_within_limits()
    {
        var name = WallpaperFileNamer.Create(CreateDetail(Tag($"A{new string('a', 400)}", "Celebrities"))).Value;

        name.ShouldBe($"A{new string('a', 99)}_abc123.jpg");
    }

    [Fact]
    public void when_the_image_url_has_no_extension_then_the_jpg_fallback_is_used()
        => WallpaperFileNamer.Create(CreateDetail("https://example.test/full/abc123", Tag("Emma Watson", "Celebrities"))).Value.ShouldBe("Emma_Watson_abc123.jpg");

    private static WallpaperTag Tag(string name, string category = "") => new(1, name, Category: category);

    private static WallpaperDetail CreateDetail(params WallpaperTag[] tags)
        => CreateDetail("https://example.test/full/abc123.jpg", tags);

    private static WallpaperDetail CreateDetail(string imageUrl, params WallpaperTag[] tags)
        => new("abc123", imageUrl, 1920, 1080, tags);
}
