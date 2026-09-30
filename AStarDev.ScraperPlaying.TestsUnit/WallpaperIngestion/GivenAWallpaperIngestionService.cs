using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;
using AStarDev.ScraperPlaying.TestsUnit.Fakes;
using AStarDev.ScraperPlaying.WallpaperIngestion;

namespace AStarDev.ScraperPlaying.TestsUnit.WallpaperIngestion;

public sealed class GivenAWallpaperIngestionService
{
    private readonly FakeRepository<FileEntity, FileId> fileRepository = new();
    private readonly FakeFilesQuery filesQuery = new();
    private readonly FakeNewWallpaperIngestor newWallpaperIngestor = new();
    private readonly CapturingProgress progress = new();
    private readonly WallpaperIngestionService service;

    public GivenAWallpaperIngestionService() => service = new(filesQuery, newWallpaperIngestor);

    [Fact]
    public async Task when_a_wallpaper_already_exists_then_it_is_skipped_and_not_ingested()
    {
        filesQuery.ExistsResult = true;

        await Ingest(CreateWallpaper("existing-wallpaper"));

        progress.Messages.ShouldContain("The file details already exist for wallpaper existing-wallpaper - no need to fetch again.");
        newWallpaperIngestor.Ingested.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_wallpaper_is_new_then_it_is_ingested_with_the_page_context_and_progress()
    {
        var wallpaper = CreateWallpaper("new-wallpaper", path: "https://example.test/full/new-wallpaper.jpg");
        using var client = new HttpClient();
        var context = new WallpaperIngestionContext("resolved-directory", client, fileRepository, "resolved-category", []);

        await service.IngestPageAsync([wallpaper], context, progress, CancellationToken.None);

        newWallpaperIngestor.Ingested.ShouldBe([(wallpaper, ".jpg", context, (IProgress<string>)progress)]);
    }

    [Fact]
    public async Task when_a_page_has_several_wallpapers_then_existence_is_checked_with_a_single_query_and_only_the_new_ones_are_ingested()
    {
        var existing = CreateWallpaper("existing-wallpaper", path: "https://example.test/full/existing-wallpaper.jpg");
        var fresh = CreateWallpaper("fresh-wallpaper", path: "https://example.test/full/fresh-wallpaper.jpg");
        var another = CreateWallpaper("another-wallpaper", path: "https://example.test/full/another-wallpaper.jpg");
        filesQuery.ExistingHandles.Add(new FileHandle("existing-wallpaper"));

        await Ingest(existing, fresh, another);

        (filesQuery.ExistingHandlesQueryCount, string.Join(",", newWallpaperIngestor.Ingested.Select(call => call.Wallpaper.Id))).ShouldBe((1, "fresh-wallpaper,another-wallpaper"));
    }

    [Fact]
    public async Task when_a_wallpaper_is_looked_up_then_its_id_is_the_handle_checked_regardless_of_its_extension()
    {
        await Ingest(CreateWallpaper("png-wallpaper", path: "https://example.test/full/png-wallpaper.png"), CreateWallpaper("extensionless-wallpaper", path: "https://example.test/full/extensionless-wallpaper"));

        string.Join(",", filesQuery.CheckedHandles.Select(handle => handle.Value)).ShouldBe("png-wallpaper,extensionless-wallpaper");
    }

    [Fact]
    public async Task when_a_wallpaper_is_new_then_the_real_extension_or_the_jpg_fallback_is_passed_on_for_the_file_name()
    {
        await Ingest(CreateWallpaper("png-wallpaper", path: "https://example.test/full/png-wallpaper.png"), CreateWallpaper("extensionless-wallpaper", path: "https://example.test/full/extensionless-wallpaper"));

        string.Join(",", newWallpaperIngestor.Ingested.Select(call => call.Extension)).ShouldBe(".png,.jpg");
    }

    [Fact]
    public async Task when_a_stored_file_has_a_person_prefixed_name_then_the_wallpaper_is_still_recognised_by_its_handle_and_not_ingested_again()
    {
        filesQuery.ExistingHandles.Add(new FileHandle("vqyxgm"));

        await Ingest(CreateWallpaper("vqyxgm", path: "https://example.test/full/vqyxgm.jpg"));

        (newWallpaperIngestor.Ingested.Count, progress.Messages.Single()).ShouldBe((0, "The file details already exist for wallpaper vqyxgm - no need to fetch again."));
    }

    [Fact]
    public async Task when_the_stored_name_differs_only_by_case_then_the_wallpaper_is_still_treated_as_existing()
    {
        filesQuery.ExistingHandles.Add(new FileHandle("mixed-case"));

        await Ingest(CreateWallpaper("Mixed-Case", path: "https://example.test/full/Mixed-Case.jpg"));

        newWallpaperIngestor.Ingested.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_page_has_no_wallpapers_then_no_query_is_made()
    {
        await Ingest();

        filesQuery.ExistingHandlesQueryCount.ShouldBe(0);
    }

    [Fact]
    public async Task when_checking_whether_wallpapers_already_exist_fails_then_the_failure_is_reported_and_the_page_is_skipped()
    {
        filesQuery.ExistsResult = new InvalidOperationException("query failed");

        await Ingest(CreateWallpaper("check-fails"));

        progress.Messages.ShouldContain("Failed to check whether the file details already exist for this page of wallpapers: query failed");
        newWallpaperIngestor.Ingested.ShouldBeEmpty();
    }

    private async Task Ingest(params Data[] wallpapers)
    {
        using var client = new HttpClient();

        await service.IngestPageAsync(wallpapers, new WallpaperIngestionContext("some-directory", client, fileRepository, "resolved-category", []), progress, CancellationToken.None);
    }

    private static Data CreateWallpaper(string id, string path = "")
        => new(id, 0, 0, 0, "", path);

    private sealed class FakeNewWallpaperIngestor : INewWallpaperIngestor
    {
        public List<(Data Wallpaper, string Extension, WallpaperIngestionContext Context, IProgress<string> Progress)> Ingested { get; } = [];

        public Task IngestAsync(Data wallpaper, string extension, WallpaperIngestionContext context, IProgress<string> progress, CancellationToken cancellationToken)
        {
            Ingested.Add((wallpaper, extension, context, progress));

            return Task.CompletedTask;
        }
    }

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }
}
