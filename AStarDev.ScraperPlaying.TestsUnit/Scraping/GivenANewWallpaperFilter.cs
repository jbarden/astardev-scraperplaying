using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;

namespace AStarDev.ScraperPlaying.TestsUnit.Scraping;

public sealed class GivenANewWallpaperFilter
{
    private readonly IFilesQuery filesQuery = Substitute.For<IFilesQuery>();
    private readonly List<string> messages = [];
    private readonly NewWallpaperFilter filter;

    public GivenANewWallpaperFilter()
        => filter = new(filesQuery);

    [Fact]
    public async Task when_none_were_downloaded_then_all_are_kept_and_nothing_is_reported()
    {
        filesQuery.GetExistingHandlesAsync(Arg.Any<IReadOnlyCollection<FileHandle>>(), Arg.Any<CancellationToken>()).Returns(Existing());

        var kept = await Filter("wallpaper-1", "wallpaper-2");

        kept.ShouldBe(["wallpaper-1", "wallpaper-2"]);
        messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_some_were_downloaded_then_only_the_new_ones_are_kept_and_each_skip_is_reported()
    {
        filesQuery.GetExistingHandlesAsync(Arg.Any<IReadOnlyCollection<FileHandle>>(), Arg.Any<CancellationToken>()).Returns(Existing("wallpaper-1"));

        var kept = await Filter("wallpaper-1", "wallpaper-2");

        kept.ShouldBe(["wallpaper-2"]);
        messages.ShouldBe(["Wallpaper wallpaper-1 was already downloaded - skipping."]);
    }

    [Fact]
    public async Task when_the_existence_check_fails_then_none_are_kept_and_the_failure_is_reported()
    {
        filesQuery.GetExistingHandlesAsync(Arg.Any<IReadOnlyCollection<FileHandle>>(), Arg.Any<CancellationToken>()).Returns((Exceptional<IReadOnlySet<FileHandle>>)new InvalidOperationException("db down"));

        var kept = await Filter("wallpaper-1");

        kept.ShouldBeEmpty();
        messages.ShouldBe(["Failed to check which wallpapers were already downloaded, skipping this page: db down"]);
    }

    private static Exceptional<IReadOnlySet<FileHandle>> Existing(params string[] wallpaperIds)
    {
        IReadOnlySet<FileHandle> handles = wallpaperIds.Select(FileHandle.Create).ToHashSet();

        return Exceptional.Success(handles);
    }

    private Task<string[]> Filter(params string[] wallpaperIds)
        => filter.ExcludeAlreadyDownloadedAsync(wallpaperIds, new Progress(messages), TestContext.Current.CancellationToken);

    private sealed class Progress(List<string> messages) : IProgress<string>
    {
        public void Report(string value) => messages.Add(value);
    }
}
