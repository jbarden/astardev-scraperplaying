using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using AStarDev.ControlDb;
using AStarDev.FunctionalParadigm;
using AStarDev.ControlDb.FileDetail;
using AStarDev.ControlDb.TagDetail;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;
using AStarDev.ScraperPlaying.Startup;
using AStarDev.ScraperPlaying.WallpaperIngestion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Testably.Abstractions.Testing;
using Xunit;

namespace AStarDev.ScraperPlaying.TestsIntegration.WallpaperIngestion;

/// <summary>Runs the real ingestor, downloader, recorder, tag fetcher and tag linker against a temp SQLite file, a mock file system and a fake HTTP handler, asserting on what ends up saved, stored and linked.</summary>
public sealed class GivenANewWallpaperIngestorOverARealDatabase : IDisposable
{
    private static readonly byte[] ImageBytes = [1, 2, 3, 4, 5];
    private static readonly string[] PersonCategories = ["Celebrities"];

    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"astardev-scraperplaying-ingestor-{Guid.CreateVersion7():N}.db");
    private readonly ServiceProvider serviceProvider;
    private readonly MockFileSystem fileSystem = new();
    private readonly ImageDownloadNotifier notifier = new();
    private readonly CapturingProgress progress = new();
    private int detailRequests;
    private bool disposed;

    public GivenANewWallpaperIngestorOverARealDatabase()
    {
        serviceProvider = new ServiceCollection().AddDataServices(databasePath).BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        _ = scope.ServiceProvider.GetRequiredService<ControlDbContext>().Database.EnsureCreated();
    }

    [Fact]
    public async Task when_a_wallpaper_is_ingested_then_the_image_is_saved_the_file_row_is_stored_and_its_tags_are_linked()
    {
        await Ingest("new-wallpaper", HttpStatusCode.OK, [WallhavenTag(1, "landscape"), WallhavenTag(2, "mountain")], TestContext.Current.CancellationToken);

        var savedPath = fileSystem.Path.Combine("save-directory", "new-wallpaper.jpg");
        (await fileSystem.File.ReadAllBytesAsync(savedPath, TestContext.Current.CancellationToken)).ShouldBe(ImageBytes);
        var stored = await ReadStoredAsync();
        stored.Files.ShouldBe(["new-wallpaper.jpg"]);
        stored.Tags.ShouldBe(["landscape", "mountain"], ignoreOrder: true);
        stored.LinkedTags.ShouldBe([("new-wallpaper.jpg", "landscape"), ("new-wallpaper.jpg", "mountain")], ignoreOrder: true);
    }

    [Fact]
    public async Task when_the_operation_is_cancelled_after_the_download_then_the_image_is_saved_but_no_rows_are_stored_or_linked()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        notifier.ImageDownloaded += (_, _) => cancellationTokenSource.Cancel();

        _ = await Should.ThrowAsync<OperationCanceledException>(() => Ingest("cancelled-wallpaper", HttpStatusCode.OK, [WallhavenTag(1, "landscape")], cancellationTokenSource.Token));

        fileSystem.File.Exists(fileSystem.Path.Combine("save-directory", "cancelled-wallpaper.jpg")).ShouldBeTrue();
        (await ReadStoredAsync()).IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_image_download_fails_then_the_failure_is_reported_and_nothing_is_saved_stored_or_linked()
    {
        await Ingest("failing-wallpaper", HttpStatusCode.InternalServerError, [WallhavenTag(1, "landscape")], TestContext.Current.CancellationToken);

        progress.Messages.ShouldContain(message => message.StartsWith("Failed to process image for wallpaper failing-wallpaper: ", StringComparison.Ordinal));
        (fileSystem.Directory.Exists("save-directory") && fileSystem.Directory.GetFiles("save-directory").Length > 0).ShouldBeFalse();
        (await ReadStoredAsync()).IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public async Task when_linking_the_tags_fails_then_the_failure_is_reported_and_no_untagged_file_row_is_stored()
    {
        await Ingest("untagged-wallpaper", HttpStatusCode.OK, [WallhavenTag(1, "landscape")], TestContext.Current.CancellationToken, linkFailure: new InvalidOperationException("link failed"));

        progress.Messages.ShouldContain("Failed to link tags for wallpaper untagged-wallpaper: link failed");
        var stored = await ReadStoredAsync();
        stored.Files.ShouldBeEmpty();
        stored.LinkedTags.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_wallpaper_is_ignored_then_it_is_recorded_once_and_the_next_scrape_does_not_fetch_its_tags_again()
    {
        await SeedIgnoredTagAsync(2, "unwanted");
        object[] tags = [WallhavenTag(1, "landscape"), WallhavenTag(2, "unwanted")];

        await IngestPage("ignored-wallpaper", tags, TestContext.Current.CancellationToken);
        var detailRequestsAfterFirstScrape = detailRequests;
        await IngestPage("ignored-wallpaper", tags, TestContext.Current.CancellationToken);

        (detailRequestsAfterFirstScrape, detailRequests).ShouldBe((1, 1));
        (await ReadIgnoredHandlesAsync()).ShouldBe(["ignored-wallpaper"]);
        (await ReadStoredAsync()).Files.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_wallpaper_is_not_ignored_then_it_is_stored_and_not_remembered_as_ignored()
    {
        await SeedIgnoredTagAsync(2, "unwanted");

        await IngestPage("kept-wallpaper", [WallhavenTag(1, "landscape")], TestContext.Current.CancellationToken);

        (await ReadStoredAsync()).Files.ShouldBe(["kept-wallpaper.jpg"]);
        (await ReadIgnoredHandlesAsync()).ShouldBeEmpty();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposed) return;

        disposed = true;

        if (!disposing) return;

        serviceProvider.Dispose();
        if (File.Exists(databasePath)) File.Delete(databasePath);
    }

    private NewWallpaperIngestor CreateIngestor(IServiceProvider scopedServices, Exception? linkFailure = null)
    {
        var unitOfWork = scopedServices.GetRequiredService<IUnitOfWork>();
        var tagsQuery = scopedServices.GetRequiredService<ITagsQuery>();
        var flagStore = new TagFlagStore();

        return new NewWallpaperIngestor(
            new TagFetcher(new JsonResponseProcessor(), tagsQuery, flagStore),
            new TagLinker(tagsQuery, unitOfWork, linkFailure is null ? scopedServices.GetRequiredService<IFileTagRepository>() : new FailingFileTagRepository(linkFailure), flagStore),
            new WallpaperSaver(new ImageDownloader(fileSystem, System.TimeProvider.System, DownloadPacing.None, ScrapeTimeouts.Default), new WallpaperFileRecorder(System.TimeProvider.System), notifier),
            scopedServices.GetRequiredService<IIgnoredWallpapers>());
    }

    private async Task IngestPage(string wallpaperId, object[] tags, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var service = new WallpaperIngestionService(scope.ServiceProvider.GetRequiredService<IFilesQuery>(), CreateIngestor(scope.ServiceProvider));
        using var client = CreateClient(HttpStatusCode.OK, tags);
        var context = new WallpaperIngestionContext(new SaveDirectories("save-directory", "famous-save-directory", ""), client, unitOfWork.GetRepository<FileEntity, FileId>(), "category", PersonCategories);

        _ = await service.IngestPageAsync([new Data(wallpaperId, 0, 0, 0, "", "https://example.test/image.jpg")], context, progress, cancellationToken);
        _ = await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task Ingest(string wallpaperId, HttpStatusCode imageStatus, object[] tags, CancellationToken cancellationToken, Exception? linkFailure = null)
    {
        using var scope = serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var ingestor = CreateIngestor(scope.ServiceProvider, linkFailure);
        using var client = CreateClient(imageStatus, tags);
        var wallpaper = new Data(wallpaperId, 0, 0, 0, "", "https://example.test/image.jpg");
        var context = new WallpaperIngestionContext(new SaveDirectories("save-directory", "famous-save-directory", ""), client, unitOfWork.GetRepository<FileEntity, FileId>(), "category", PersonCategories);

        var fetchedTags = await ingestor.FetchTagsAsync(wallpaper, context, progress, cancellationToken);
        if (fetchedTags.TryGetValue(out var tagsToLink)) await ingestor.IngestAsync(new WallpaperCandidate(wallpaper, ".jpg"), tagsToLink, context, progress, cancellationToken);
        _ = await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<StoredRows> ReadStoredAsync()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ControlDbContext>();
        var files = await context.Files.ToListAsync(TestContext.Current.CancellationToken);
        var tags = await context.Tags.ToListAsync(TestContext.Current.CancellationToken);
        var links = await context.FileTags.ToListAsync(TestContext.Current.CancellationToken);

        return new StoredRows(
            [.. files.Select(file => file.FileName.Value)],
            [.. tags.Select(tag => tag.Name)],
            [.. links.Select(link => (files.Single(file => file.Id == link.FileId).FileName.Value, tags.Single(tag => tag.Id == link.TagId).Name))]);
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient owns and disposes the handler.")]
    private HttpClient CreateClient(HttpStatusCode imageStatus, object[] tags)
        => new(new StubHttpMessageHandler(request => Respond(request, imageStatus, tags))) { BaseAddress = new Uri("https://example.test/") };

    private HttpResponseMessage Respond(HttpRequestMessage request, HttpStatusCode imageStatus, object[] tags)
    {
        if (!IsDetailRequest(request)) return new HttpResponseMessage(imageStatus) { Content = new ByteArrayContent(ImageBytes) };

        _ = Interlocked.Increment(ref detailRequests);

        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new { data = new { tags } }), System.Text.Encoding.UTF8, "application/json") };
    }

    private static bool IsDetailRequest(HttpRequestMessage request)
        => request.RequestUri!.AbsolutePath.StartsWith($"/{ApplicationConstants.WallhavenDetailPathTemplate}", StringComparison.Ordinal);

    private async Task SeedIgnoredTagAsync(int wallhavenTagId, string name)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ControlDbContext>();
        _ = await context.Tags.AddAsync(new TagEntity { WallhavenTagId = wallhavenTagId, Name = name, IgnoreImage = true }, TestContext.Current.CancellationToken);
        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task<IReadOnlyList<string>> ReadIgnoredHandlesAsync()
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ControlDbContext>();

        return [.. (await context.IgnoredWallpapers.ToListAsync(TestContext.Current.CancellationToken)).Select(ignored => ignored.FileHandle.Value)];
    }

    private static object WallhavenTag(int id, string name)
        => new { id, name, alias = name, category_id = 1, category = "Nature", purity = "sfw" };

    private sealed record StoredRows(IReadOnlyList<string> Files, IReadOnlyList<string> Tags, IReadOnlyList<(string FileName, string TagName)> LinkedTags)
    {
        public bool IsEmpty => Files.Count + Tags.Count + LinkedTags.Count == 0;
    }

    private sealed class FailingFileTagRepository(Exception failure) : IFileTagRepository
    {
        public Exceptional<FileTagEntity> Add(FileTagEntity fileTag) => failure;
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }
}
