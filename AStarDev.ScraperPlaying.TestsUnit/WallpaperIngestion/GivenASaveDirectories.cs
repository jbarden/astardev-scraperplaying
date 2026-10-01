using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.DetailResponse;
using AStarDev.ScraperPlaying.WallpaperIngestion;

namespace AStarDev.ScraperPlaying.TestsUnit.WallpaperIngestion;

public sealed class GivenASaveDirectories
{
    private static readonly SaveDirectories Directories = new("normal", "famous");

    [Fact]
    public void when_the_wallpaper_has_no_tags_then_the_normal_directory_is_used()
        => Directories.For([]).ShouldBe("normal");

    [Fact]
    public void when_no_tag_is_famous_then_the_normal_directory_is_used()
        => Directories.For([Tag(famous: false), Tag(famous: false)]).ShouldBe("normal");

    [Fact]
    public void when_any_tag_is_famous_then_the_famous_directory_is_used()
        => Directories.For([Tag(famous: false), Tag(famous: true)]).ShouldBe("famous");

    private static Tag Tag(bool famous) => new(1, "name", "name", 1, "Category", "sfw", IsFamous: famous);
}
