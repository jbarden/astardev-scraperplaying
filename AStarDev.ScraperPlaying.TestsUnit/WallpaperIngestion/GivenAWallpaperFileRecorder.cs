using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.WallpaperIngestion;

namespace AStarDev.ScraperPlaying.TestsUnit.WallpaperIngestion;

public sealed class GivenAWallpaperFileRecorder
{
    private static readonly DateTimeOffset now = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    private readonly IRepository<FileEntity, FileId> fileRepository = Substitute.For<IRepository<FileEntity, FileId>>();
    private readonly WallpaperFileRecorder recorder;

    public GivenAWallpaperFileRecorder()
    {
        fileRepository.Add(Arg.Any<FileEntity>()).Returns(call => (Exceptional<FileEntity>)call.Arg<FileEntity>());
        recorder = new(() => now);
    }

    [Fact]
    public void when_a_wallpaper_is_recorded_then_a_matching_file_entity_is_added_and_returned()
    {
        var result = recorder.Record(CreateDetail(), CreateImage(), "some-directory", fileRepository);

        var entity = result.Match(file => file, ex => throw ex);
        entity.FileName.Value.ShouldBe("wallpaper-1.png");
        entity.DirectoryName.Value.ShouldBe("some-directory");
        entity.FileHandle.Value.ShouldBe("wallpaper-1");
        entity.FileSize.ShouldBe(5);
        entity.FileType.ShouldBe("image/png");
        entity.IsImage.ShouldBeTrue();
        entity.ImageDetail!.Width.ShouldBe(1920);
        entity.ImageDetail.Height.ShouldBe(1080);
        entity.FileAccessDetail.DetailsLastUpdated.ShouldBe(now.UtcDateTime);
        fileRepository.Received(1).Add(entity);
    }

    [Fact]
    public void when_adding_the_file_entity_fails_then_the_failure_is_returned()
    {
        var exception = new InvalidOperationException("insert failed");
        fileRepository.Add(Arg.Any<FileEntity>()).Returns((Exceptional<FileEntity>)exception);

        var result = recorder.Record(CreateDetail(), CreateImage(), "some-directory", fileRepository);

        result.Match(_ => (Exception?)null, ex => ex).ShouldBeSameAs(exception);
    }

    private static WallpaperDetail CreateDetail()
        => new("wallpaper-1", "https://example.test/full/wallpaper-1.png", 1920, 1080, []);

    private static DownloadedWallpaperImage CreateImage()
        => new(new FileName("wallpaper-1.png"), "some-directory/wallpaper-1.png", 5, "image/png");
}
