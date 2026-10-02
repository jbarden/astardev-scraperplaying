using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.SearchResponse;
using AStarDev.ScraperPlaying.TestsUnit.Fakes;
using AStarDev.ScraperPlaying.WallpaperIngestion;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ScraperPlaying.TestsUnit.Scraping;

public sealed class GivenAPageIngestionStep : IDisposable
{
    private readonly HttpClient client = new();
    private readonly FakeUnitOfWork unitOfWork = new();
    private readonly RecordingIngestionService ingestionService = new();
    private readonly List<string> events = [];
    private readonly List<string> messages = [];
    private readonly PageIngestionStep step;

    public GivenAPageIngestionStep()
    {
        _ = unitOfWork.Register<FileEntity, FileId>();
        unitOfWork.OnSave = _ => events.Add("save");
        ingestionService.OnIngest = () => events.Add("ingest");
        step = new(ingestionService, unitOfWork);
    }

    [Fact]
    public async Task when_a_page_is_ingested_then_the_wallpapers_are_ingested_the_progress_reported_and_the_page_saved_in_that_order()
    {
        await step.IngestPageAsync(CreateRun(progress => events.Add($"hook {progress.LastKnownImageCount}/{progress.LastPageVisited}/{progress.TotalPages}")), new FetchedPage(2, new SearchResponse([new Data("w", 0, 0, 0, "", "")], new Meta(5, 40))), recordProgress: true);

        events.ShouldBe(["ingest", "hook 40/2/5", "save"]);
    }

    [Fact]
    public async Task when_a_wallpaper_on_the_page_was_not_ingested_then_no_progress_is_reported_but_the_page_is_still_saved_and_reported_incomplete()
    {
        ingestionService.Outcome = IngestOutcome.Incomplete;

        var outcome = await step.IngestPageAsync(CreateRun(progress => events.Add("hook")), new FetchedPage(2, new SearchResponse([new Data("w", 0, 0, 0, "", "")], new Meta(5, 40))), recordProgress: true);

        outcome.ShouldBe(IngestOutcome.Incomplete);
        events.ShouldBe(["ingest", "save"]);
    }

    [Fact]
    public async Task when_progress_is_being_withheld_then_a_fully_ingested_page_does_not_report_progress()
    {
        var outcome = await step.IngestPageAsync(CreateRun(progress => events.Add("hook")), new FetchedPage(3, new SearchResponse([new Data("w", 0, 0, 0, "", "")], new Meta(5, 40))), recordProgress: false);

        outcome.ShouldBe(IngestOutcome.Complete);
        events.ShouldBe(["ingest", "save"]);
    }

    [Fact]
    public async Task when_partially_ingested_wallpapers_are_saved_then_the_save_ignores_cancellation_and_is_reported()
    {
        await step.SavePartiallyIngestedPageAsync(new Progress(messages));

        unitOfWork.SaveTokens.Single().CanBeCanceled.ShouldBeFalse();
        messages.ShouldBe(["Scrape cancelled - saved wallpapers downloaded so far this page."]);
    }

    [Fact]
    public async Task when_saving_partially_ingested_wallpapers_fails_then_the_failure_is_reported_not_thrown()
    {
        unitOfWork.OnSave = _ => throw new DbUpdateException("save failed");

        await step.SavePartiallyIngestedPageAsync(new Progress(messages));

        messages.ShouldBe(["Scrape cancelled - failed to save wallpapers downloaded so far this page: save failed"]);
    }

    [Fact]
    public async Task when_saving_partially_ingested_wallpapers_fails_for_any_other_reason_then_the_failure_is_reported_not_thrown()
    {
        unitOfWork.OnSave = _ => throw new InvalidOperationException("context disposed");

        await step.SavePartiallyIngestedPageAsync(new Progress(messages));

        messages.ShouldBe(["Scrape cancelled - failed to save wallpapers downloaded so far this page: context disposed"]);
    }

    public void Dispose() => client.Dispose();

    private IngestionRun CreateRun(Action<SearchCategoryProgress> onPageCompleted)
        => new(
            new PageScrapeRequest(new ScrapeLabel("label", Option.None<string>()), Option.None<SearchCategoryProgress>(), new PageHooks(onPageCompleted, page => new Uri($"https://example.test/page/{page}")), new ScrapeTarget(new WallhavenConnection("api-key", new Uri("https://example.test")), [])),
            new WallpaperIngestionContext(new SaveDirectories("root", "famous", "segment"), client, new FakeRepository<FileEntity, FileId>(), "Top Wallpapers", []),
            new Progress([]),
            CancellationToken.None);

    private sealed class Progress(List<string> messages) : IProgress<string>
    {
        public void Report(string value) => messages.Add(value);
    }

    private sealed class RecordingIngestionService : IWallpaperIngestionService
    {
        public Action OnIngest { get; set; } = () => { };

        public IngestOutcome Outcome { get; set; } = IngestOutcome.Complete;

        public Task<IngestOutcome> IngestPageAsync(IReadOnlyList<Data> wallpapers, WallpaperIngestionContext context, IProgress<string> progress, CancellationToken cancellationToken)
        {
            OnIngest();

            return Task.FromResult(Outcome);
        }
    }
}
