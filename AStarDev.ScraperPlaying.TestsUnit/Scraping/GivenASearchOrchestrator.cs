using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.TestsUnit.Fakes;

namespace AStarDev.ScraperPlaying.TestsUnit.Scraping;

public sealed class GivenASearchOrchestrator
{
    private static readonly string[] expectedPersonCategories = ["Celebrities", "Models"];
    private static readonly WallhavenConnection ExpectedConnection = new("api-key", new Uri("https://example.test"));
    private readonly FakePagesProcessor pagesProcessor = new();
    private readonly CapturingProgress progress = new();
    private readonly SearchOrchestrator orchestrator;

    public GivenASearchOrchestrator() => orchestrator = new(pagesProcessor);

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

    private Task Run(ScrapeConfigurationEntity configuration) => orchestrator.RunSearchesAsync(configuration, progress, CancellationToken.None);

    private sealed record PagesCall(string LogLabel, Option<string> CategoryName, WallhavenConnection Connection, IReadOnlyList<string> PersonCategories, IProgress<string> Progress);

    private sealed class FakePagesProcessor : IPagesProcessor
    {
        public List<PagesCall> Calls { get; } = [];

        public Action OnFetch { get; set; } = () => { };

        public Task FetchAndProcessPagesAsync(string logLabel, Option<string> categoryName, Func<int, Uri> pageUrlFactory, WallhavenConnection connection, IReadOnlyList<string> personCategories, IProgress<string> progress, CancellationToken cancellationToken)
        {
            Calls.Add(new PagesCall(logLabel, categoryName, connection, personCategories, progress));
            OnFetch();

            return Task.CompletedTask;
        }
    }

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }
}
