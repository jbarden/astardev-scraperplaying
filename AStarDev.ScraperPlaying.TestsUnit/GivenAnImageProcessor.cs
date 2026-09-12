using System.Net;
using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Home;
using AStarDev.ScraperPlaying.SearchAPI.SearchResponse;
using Testably.Abstractions.Testing;

namespace AStarDev.ScraperPlaying.TestsUnit;

public sealed class GivenAnImageProcessor
{
    private static readonly DateTimeOffset now = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    private readonly IRepository<FileEntity, FileId> fileRepository = Substitute.For<IRepository<FileEntity, FileId>>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IRepository<ScrapeConfigurationEntity, ScrapeConfigurationId> scrapeConfigurationRepository = Substitute.For<IRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>>();
    private readonly MockFileSystem fileSystem = new();
    private readonly ImageProcessor processor;

    public GivenAnImageProcessor()
    {
        unitOfWork.GetRepository<ScrapeConfigurationEntity, ScrapeConfigurationId>().Returns(scrapeConfigurationRepository);
        scrapeConfigurationRepository.TryGetFirstAsync().Returns((Exceptional<Option<ScrapeConfigurationEntity>>)(Option<ScrapeConfigurationEntity>)CreateConfiguration("root-directory"));
        processor = new(() => now, fileSystem, () => TimeSpan.FromMilliseconds(1), unitOfWork);
    }

    [Fact]
    public async Task when_processing_a_wallpaper_then_a_matching_file_entity_is_added_and_returned()
    {
        fileRepository.Add(Arg.Any<FileEntity>()).Returns(call => (Exceptional<FileEntity>)call.Arg<FileEntity>());
        var wallpaper = CreateWallpaper(id: "wallpaper-1", fileSize: 1234, fileType: "image/jpeg", dimensionX: 1920, dimensionY: 1080, path: "https://example.test/full/wallpaper-1.jpg");

        var result = await processor.ProcessTheImageAsync(fileRepository, wallpaper, Option.None<string>(), CancellationToken.None);

        var addedEntity = result.Match(entity => entity, _ => (FileEntity?)null);
        addedEntity.ShouldNotBeNull();
        addedEntity.FileName.Value.ShouldBe("wallpaper-1");
        addedEntity.FileHandle.Value.ShouldBe("wallpaper-1");
        addedEntity.FileSize.ShouldBe(1234);
        addedEntity.FileType.ShouldBe("image/jpeg");
        addedEntity.IsImage.ShouldBeTrue();
        addedEntity.ImageDetail!.Width.ShouldBe(1920);
        addedEntity.ImageDetail!.Height.ShouldBe(1080);
        addedEntity.FileAccessDetail.DetailsLastUpdated.ShouldBe(now.UtcDateTime);
    }

    [Fact]
    public async Task when_no_category_name_is_supplied_then_the_directory_is_the_root_combined_with_top_wallpapers()
    {
        fileRepository.Add(Arg.Any<FileEntity>()).Returns(call => (Exceptional<FileEntity>)call.Arg<FileEntity>());
        var wallpaper = CreateWallpaper(id: "wallpaper-top");

        var result = await processor.ProcessTheImageAsync(fileRepository, wallpaper, Option.None<string>(), CancellationToken.None);

        var addedEntity = result.Match(entity => entity, _ => (FileEntity?)null);
        addedEntity.ShouldNotBeNull();
        addedEntity.DirectoryName.Value.ShouldBe(fileSystem.Path.Combine("root-directory", "top-wallpapers"));
    }

    [Fact]
    public async Task when_a_category_name_is_supplied_then_the_directory_is_the_root_combined_with_the_slugified_category_name()
    {
        fileRepository.Add(Arg.Any<FileEntity>()).Returns(call => (Exceptional<FileEntity>)call.Arg<FileEntity>());
        var wallpaper = CreateWallpaper(id: "wallpaper-category");

        var result = await processor.ProcessTheImageAsync(fileRepository, wallpaper, Option.Some("My Category"), CancellationToken.None);

        var addedEntity = result.Match(entity => entity, _ => (FileEntity?)null);
        addedEntity.ShouldNotBeNull();
        addedEntity.DirectoryName.Value.ShouldBe(fileSystem.Path.Combine("root-directory", "my-category"));
    }

    [Fact]
    public async Task when_the_wallpaper_path_does_not_have_an_image_extension_then_is_image_is_false()
    {
        fileRepository.Add(Arg.Any<FileEntity>()).Returns(call => (Exceptional<FileEntity>)call.Arg<FileEntity>());
        var wallpaper = CreateWallpaper(id: "wallpaper-5", path: "https://example.test/full/wallpaper-5.txt");

        var result = await processor.ProcessTheImageAsync(fileRepository, wallpaper, Option.None<string>(), CancellationToken.None);

        var addedEntity = result.Match(entity => entity, _ => (FileEntity?)null);
        addedEntity.ShouldNotBeNull();
        addedEntity.IsImage.ShouldBeFalse();
    }

    [Fact]
    public async Task when_adding_the_file_entity_fails_then_the_failure_is_returned_not_swallowed()
    {
        var exception = new InvalidOperationException("add failed");
        fileRepository.Add(Arg.Any<FileEntity>()).Returns((Exceptional<FileEntity>)exception);
        var wallpaper = CreateWallpaper(id: "wallpaper-2");

        var result = await processor.ProcessTheImageAsync(fileRepository, wallpaper, Option.None<string>(), CancellationToken.None);

        var capturedException = result.Match(_ => (Exception?)null, ex => ex);
        capturedException.ShouldBeSameAs(exception);
    }

    [Fact]
    public async Task when_downloading_an_image_succeeds_then_it_is_written_to_the_expected_file_and_progress_is_reported()
    {
        var imageBytes = new byte[] { 1, 2, 3, 4, 5 };
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(imageBytes) });
        var progress = new CapturingProgress();

        await processor.DownloadImageAsync("wallpaper-3", "https://example.test/image.jpg", progress, client, CancellationToken.None);

        fileSystem.File.Exists("wallpaper-3.jpg").ShouldBeTrue();
        fileSystem.File.ReadAllBytes("wallpaper-3.jpg").ShouldBe(imageBytes);
        progress.Messages.ShouldContain("Downloading image for wallpaper wallpaper-3 from https://example.test/image.jpg");
    }

    [Fact]
    public async Task when_downloading_an_image_receives_a_non_success_status_then_it_throws_and_writes_no_file()
    {
        using var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var progress = new CapturingProgress();

        await Should.ThrowAsync<HttpRequestException>(
            () => processor.DownloadImageAsync("wallpaper-4", "https://example.test/image.jpg", progress, client, CancellationToken.None));

        fileSystem.File.Exists("wallpaper-4.jpg").ShouldBeFalse();
    }

    private static Data CreateWallpaper(string id, int fileSize = 0, string fileType = "", int dimensionX = 0, int dimensionY = 0, string path = "")
        => new(id, "", "", 0, 0, "", "", "", dimensionX, dimensionY, "", "", fileSize, fileType, "", [], path, new Thumbs("", "", ""));

    private static ScrapeConfigurationEntity CreateConfiguration(string rootDirectory)
    {
        var scrapeConfigurationId = new ScrapeConfigurationId(Guid.CreateVersion7());

        return new ScrapeConfigurationEntity(scrapeConfigurationId)
        {
            ScrapeDirectories = new ScrapeDirectoriesEntity(new ScrapeDirectoriesId(Guid.CreateVersion7()), scrapeConfigurationId, rootDirectory, rootDirectory, "")
        };
    }

    private static HttpClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> responder)
        => new(new StubHttpMessageHandler(responder));

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }
}
