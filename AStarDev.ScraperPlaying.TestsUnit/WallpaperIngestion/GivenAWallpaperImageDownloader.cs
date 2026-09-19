using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.WallpaperIngestion;
using Microsoft.Playwright;
using Testably.Abstractions.Testing;

namespace AStarDev.ScraperPlaying.TestsUnit.WallpaperIngestion;

public sealed class GivenAWallpaperImageDownloader
{
    private static readonly byte[] imageBytes = [1, 2, 3, 4, 5];
    private readonly IPage page = Substitute.For<IPage>();
    private readonly IAPIRequestContext requestContext = Substitute.For<IAPIRequestContext>();
    private readonly IAPIResponse response = Substitute.For<IAPIResponse>();
    private readonly MockFileSystem fileSystem = new();
    private readonly CapturingProgress progress = new();
    private readonly WallpaperImageDownloader downloader;

    public GivenAWallpaperImageDownloader()
    {
        var browserContext = Substitute.For<IBrowserContext>();
        page.Context.Returns(browserContext);
        browserContext.APIRequest.Returns(requestContext);
        requestContext.GetAsync(Arg.Any<string>(), Arg.Any<APIRequestContextOptions?>()).Returns(response);
        response.Ok.Returns(true);
        response.Status.Returns(200);
        response.BodyAsync().Returns(imageBytes);
        response.Headers.Returns(new Dictionary<string, string> { ["content-type"] = "image/png" });
        downloader = new(fileSystem);
    }

    [Fact]
    public async Task when_the_image_is_downloaded_then_it_is_requested_with_the_browser_session_and_saved()
    {
        await Run(CreateDetail());

        await requestContext.Received(1).GetAsync("https://example.test/full/wallpaper-1.png", Arg.Any<APIRequestContextOptions?>());
        fileSystem.File.ReadAllBytes(fileSystem.Path.Combine("some-directory", "wallpaper-1.png")).ShouldBe(imageBytes);
    }

    [Fact]
    public async Task when_the_image_is_downloaded_then_the_saved_image_details_are_returned_and_progress_reported()
    {
        var result = await Run(CreateDetail());

        var image = result.Match(downloaded => downloaded, ex => throw ex);
        image.FileName.Value.ShouldBe("wallpaper-1.png");
        image.SavedPath.ShouldBe(fileSystem.Path.Combine("some-directory", "wallpaper-1.png"));
        image.SizeBytes.ShouldBe(imageBytes.Length);
        image.ContentType.ShouldBe("image/png");
        progress.Messages.ShouldContain("Downloading image for wallpaper wallpaper-1 from https://example.test/full/wallpaper-1.png");
    }

    [Fact]
    public async Task when_the_response_has_no_content_type_then_the_content_type_is_empty()
    {
        response.Headers.Returns(new Dictionary<string, string>());

        var result = await Run(CreateDetail());

        result.Match(downloaded => downloaded, ex => throw ex).ContentType.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task when_the_image_url_has_no_extension_then_the_jpg_fallback_is_used()
    {
        var result = await Run(CreateDetail(imageUrl: "https://example.test/full/wallpaper-1"));

        result.Match(downloaded => downloaded, ex => throw ex).FileName.Value.ShouldBe("wallpaper-1.jpg");
    }

    [Fact]
    public async Task when_the_image_url_is_empty_then_a_failure_is_returned_and_nothing_is_downloaded()
    {
        var result = await Run(CreateDetail(imageUrl: string.Empty));

        result.Match(_ => (Exception?)null, ex => ex).ShouldBeOfType<InvalidOperationException>();
        await requestContext.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<APIRequestContextOptions?>());
    }

    [Fact]
    public async Task when_the_download_receives_a_non_success_status_then_a_failure_is_returned_and_no_file_is_written()
    {
        response.Ok.Returns(false);
        response.Status.Returns(403);

        var result = await Run(CreateDetail());

        result.Match(_ => (Exception?)null, ex => ex).ShouldBeOfType<HttpRequestException>();
        fileSystem.File.Exists(fileSystem.Path.Combine("some-directory", "wallpaper-1.png")).ShouldBeFalse();
    }

    private Task<Exceptional<DownloadedWallpaperImage>> Run(WallpaperDetail detail)
        => downloader.DownloadAsync(detail, page, "some-directory", progress, TestContext.Current.CancellationToken);

    private static WallpaperDetail CreateDetail(string imageUrl = "https://example.test/full/wallpaper-1.png")
        => new("wallpaper-1", imageUrl, 1920, 1080, []);

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }
}
