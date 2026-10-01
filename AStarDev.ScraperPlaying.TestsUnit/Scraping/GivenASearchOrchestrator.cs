using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.TestsUnit.Fakes;

namespace AStarDev.ScraperPlaying.TestsUnit.Scraping;

public sealed class GivenASearchOrchestrator
{
    private static readonly string[] expectedPersonCategories = ["Celebrities", "Models"];
    private static readonly WallhavenConnection ExpectedConnection = new("api-key", new Uri("https://example.test"));
    private readonly FakePagesProcessor pagesProcessor = new();
    private readonly FakeUnitOfWork unitOfWork = new();
    private readonly CapturingProgress progress = new();
    private readonly SearchOrchestrator orchestrator;

    public GivenASearchOrchestrator() => orchestrator = new(pagesProcessor, unitOfWork, new ScrapeLimits(3, 4));

    [Fact]
    public async Task when_a_configuration_has_many_categories_then_up_to_three_categories_then_top_wallpapers_are_processed()
    {
        await Run(ScrapeConfigurationTestData.CreateConfiguration(categoryCount: 5));

        progress.Messages.ShouldContain("Fetching categories.");
        progress.Messages.ShouldContain("Fetching top wallpapers.");
        pagesProcessor.Calls.Select(call => (call.LogLabel, call.CategoryName)).ShouldBe([
            ("search category category one", Option.Some("category one")),
            ("search category category two", Option.Some("category two")),
            ("search category category three", Option.Some("category three")),
            ("top wallpapers", Option.None<string>())
        ]);
    }

    [Fact]
    public async Task when_the_default_limits_are_used_then_every_category_is_processed()
    {
        var unlimitedOrchestrator = new SearchOrchestrator(pagesProcessor, unitOfWork, ScrapeLimits.Default);

        await unlimitedOrchestrator.RunSearchesAsync(ScrapeConfigurationTestData.CreateConfiguration(categoryCount: 5), progress, CancellationToken.None);

        pagesProcessor.Calls.Count.ShouldBe(6);
    }

    [Fact]
    public async Task when_the_category_limit_is_lower_then_only_that_many_categories_are_processed()
    {
        var limitedOrchestrator = new SearchOrchestrator(pagesProcessor, unitOfWork, new ScrapeLimits(1, 4));

        await limitedOrchestrator.RunSearchesAsync(ScrapeConfigurationTestData.CreateConfiguration(categoryCount: 5), progress, CancellationToken.None);

        pagesProcessor.Calls.Select(call => call.LogLabel).ShouldBe(["search category category one", "top wallpapers"]);
    }

    [Fact]
    public async Task when_pages_are_processed_then_every_call_uses_the_configured_connection_person_categories_and_progress()
    {
        await Run(ScrapeConfigurationTestData.CreateConfiguration(categoryCount: 2));

        pagesProcessor.Calls.ShouldAllBe(call => call.Connection == ExpectedConnection);
        pagesProcessor.Calls.ShouldAllBe(call => call.PersonCategories.SequenceEqual(expectedPersonCategories));
        pagesProcessor.Calls.ShouldAllBe(call => ReferenceEquals(call.Progress, progress));
    }

    [Fact]
    public async Task when_a_configuration_has_no_categories_then_only_the_top_wallpapers_are_processed()
    {
        await Run(ScrapeConfigurationTestData.CreateConfiguration(categoryCount: 0));

        pagesProcessor.Calls.Select(call => call.LogLabel).ShouldBe(["top wallpapers"]);
    }

    [Fact]
    public async Task when_a_search_fails_then_the_failure_propagates_and_later_searches_are_not_run()
    {
        pagesProcessor.OnFetch = () => throw new HttpRequestException("boom");

        _ = await Should.ThrowAsync<HttpRequestException>(() => Run(ScrapeConfigurationTestData.CreateConfiguration(categoryCount: 2)));

        pagesProcessor.Calls.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_a_category_is_scraped_then_its_progress_is_recorded_and_saved_before_the_next_search()
    {
        var configuration = ScrapeConfigurationTestData.CreateConfiguration(categoryCount: 1);
        pagesProcessor.Result = Option.Some(new SearchCategoryProgress(50, 4, 10));

        await Run(configuration);

        var category = configuration.SearchConfiguration.SearchCategories.Single();
        (category.LastKnownImageCount, category.LastPageVisited, category.TotalPages, unitOfWork.SaveCount).ShouldBe((50, 4, 10, 1));
    }

    [Fact]
    public async Task when_a_category_is_skipped_then_its_progress_is_unchanged_and_nothing_is_saved()
    {
        var configuration = ScrapeConfigurationTestData.CreateConfiguration(categoryCount: 1);
        configuration.SearchConfiguration.SearchCategories.Single().RecordScrapeProgress(7, 3, 9);
        pagesProcessor.Result = Option.None<SearchCategoryProgress>();

        await Run(configuration);

        var category = configuration.SearchConfiguration.SearchCategories.Single();
        (category.LastKnownImageCount, category.LastPageVisited, category.TotalPages, unitOfWork.SaveCount).ShouldBe((7, 3, 9, 0));
    }

    [Fact]
    public async Task when_a_category_has_stored_progress_then_it_is_passed_to_the_pages_processor_and_top_wallpapers_get_none()
    {
        var configuration = ScrapeConfigurationTestData.CreateConfiguration(categoryCount: 1);
        configuration.SearchConfiguration.SearchCategories.Single().RecordScrapeProgress(7, 3, 9);

        await Run(configuration);

        pagesProcessor.Calls.Select(call => call.PreviousProgress).ShouldBe([Option.Some(new SearchCategoryProgress(7, 3, 9)), Option.None<SearchCategoryProgress>()]);
    }

    [Fact]
    public async Task when_a_page_completes_then_the_category_progress_is_recorded_on_the_category()
    {
        var configuration = ScrapeConfigurationTestData.CreateConfiguration(categoryCount: 1);
        pagesProcessor.PageProgress.Add(new SearchCategoryProgress(50, 2, 10));

        await Run(configuration);

        var category = configuration.SearchConfiguration.SearchCategories.Single();
        (category.LastKnownImageCount, category.LastPageVisited, category.TotalPages).ShouldBe((50, 2, 10));
    }

    [Fact]
    public async Task when_top_wallpaper_pages_complete_then_no_category_progress_is_recorded()
    {
        var configuration = ScrapeConfigurationTestData.CreateConfiguration(categoryCount: 0);
        pagesProcessor.PageProgress.Add(new SearchCategoryProgress(50, 2, 10));

        await Run(configuration);

        unitOfWork.SaveCount.ShouldBe(0);
    }

    private Task Run(ScrapeConfigurationEntity configuration) => orchestrator.RunSearchesAsync(configuration, progress, CancellationToken.None);

    private sealed record PagesCall(string LogLabel, Option<string> CategoryName, Option<SearchCategoryProgress> PreviousProgress, WallhavenConnection Connection, IReadOnlyList<string> PersonCategories, IProgress<string> Progress);

    private sealed class FakePagesProcessor : IPagesProcessor
    {
        public List<PagesCall> Calls { get; } = [];

        public List<SearchCategoryProgress> PageProgress { get; } = [];

        public Action OnFetch { get; set; } = () => { };

        public Option<SearchCategoryProgress> Result { get; set; } = Option.None<SearchCategoryProgress>();

        public Task<Option<SearchCategoryProgress>> FetchAndProcessPagesAsync(string logLabel, Option<string> categoryName, Option<SearchCategoryProgress> previousProgress, Action<SearchCategoryProgress> onPageCompleted, Func<int, Uri> pageUrlFactory, WallhavenConnection connection, IReadOnlyList<string> personCategories, IProgress<string> progress, CancellationToken cancellationToken)
        {
            Calls.Add(new PagesCall(logLabel, categoryName, previousProgress, connection, personCategories, progress));
            OnFetch();
            PageProgress.ForEach(onPageCompleted);

            return Task.FromResult(Result);
        }
    }

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }
}
