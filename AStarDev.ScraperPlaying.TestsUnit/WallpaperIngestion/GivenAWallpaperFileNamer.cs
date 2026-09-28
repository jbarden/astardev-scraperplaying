using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.WallpaperIngestion;

namespace AStarDev.ScraperPlaying.TestsUnit.WallpaperIngestion;

public sealed class GivenAWallpaperFileNamer
{
    [Fact]
    public void when_the_wallpaper_has_no_famous_tag_then_the_wallpaper_id_is_the_file_name()
        => WallpaperFileNamer.Create(CreateDetail(Tag("anime"), Tag("Emma Watson"))).Value.ShouldBe("abc123.jpg");

    [Fact]
    public void when_the_wallpaper_has_no_tags_then_the_wallpaper_id_is_the_file_name()
        => WallpaperFileNamer.Create(CreateDetail()).Value.ShouldBe("abc123.jpg");

    [Theory]
    [InlineData("actress")]
    [InlineData("Model")]
    [InlineData("female singer")]
    public void when_the_wallpaper_has_a_famous_tag_then_the_other_tag_names_prefix_the_file_name(string famousTag)
        => WallpaperFileNamer.Create(CreateDetail(Tag(famousTag), Tag("Emma Watson"))).Value.ShouldBe("Emma_Watson_abc123.jpg");

    [Fact]
    public void when_the_wallpaper_has_several_other_tags_then_they_are_all_joined_in_page_order()
        => WallpaperFileNamer.Create(CreateDetail(Tag("brunette"), Tag("actress"), Tag("Emma Watson"), Tag("model"))).Value.ShouldBe("brunette_Emma_Watson_abc123.jpg");

    [Fact]
    public void when_the_wallpaper_has_only_famous_tags_then_the_wallpaper_id_is_the_file_name()
        => WallpaperFileNamer.Create(CreateDetail(Tag("actress"), Tag("model"))).Value.ShouldBe("abc123.jpg");

    [Fact]
    public void when_a_tag_name_has_invalid_file_name_characters_then_they_are_removed()
        => WallpaperFileNamer.Create(CreateDetail(Tag("actress"), Tag("AC/DC: \"live\"?"))).Value.ShouldBe("ACDC_live_abc123.jpg");

    [Fact]
    public void when_a_tag_name_is_only_invalid_file_name_characters_then_it_is_skipped()
        => WallpaperFileNamer.Create(CreateDetail(Tag("actress"), Tag("???"))).Value.ShouldBe("abc123.jpg");

    [Fact]
    public void when_the_prefix_is_very_long_then_it_is_truncated_so_the_file_name_stays_within_limits()
    {
        var name = WallpaperFileNamer.Create(CreateDetail(Tag("actress"), Tag(new string('a', 400)))).Value;

        name.ShouldBe($"{new string('a', 100)}_abc123.jpg");
    }

    [Fact]
    public void when_the_image_url_has_no_extension_then_the_jpg_fallback_is_used()
        => WallpaperFileNamer.Create(CreateDetail("https://example.test/full/abc123", Tag("actress"), Tag("Emma Watson"))).Value.ShouldBe("Emma_Watson_abc123.jpg");

    private static WallpaperTag Tag(string name) => new(1, name);

    private static WallpaperDetail CreateDetail(params WallpaperTag[] tags)
        => CreateDetail("https://example.test/full/abc123.jpg", tags);

    private static WallpaperDetail CreateDetail(string imageUrl, params WallpaperTag[] tags)
        => new("abc123", imageUrl, 1920, 1080, tags);
}
