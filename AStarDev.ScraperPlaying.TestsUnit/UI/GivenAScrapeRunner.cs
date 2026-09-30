using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.UI;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.ScraperPlaying.TestsUnit.UI;

public sealed class GivenAScrapeRunner
{
    private readonly FakeScrapeService scrapeService = new();
    private readonly StatusReporter status = new(NullLogger<StatusReporter>.Instance);
    private readonly ScrapeRunner runner;

    public GivenAScrapeRunner() => runner = new(scrapeService, status);

    [Fact]
    public async Task when_the_scrape_reports_progress_then_each_message_is_appended_to_the_status()
    {
        scrapeService.OnRun = progress =>
        {
            progress.Report("Starting scrape operation.");
            progress.Report("Fetching categories.");
        };

        await runner.RunAsync();

        status.Text.ShouldBe($"Starting scrape operation.{Environment.NewLine}Fetching categories.");
    }

    [Fact]
    public async Task when_the_scrape_throws_then_the_failure_is_reported_and_not_thrown()
    {
        scrapeService.OnRun = _ => throw new InvalidOperationException("Scrape configuration not found");

        await Should.NotThrowAsync(() => runner.RunAsync());

        status.Text.ShouldBe("The scrape failed. Scrape configuration not found");
    }

    private sealed class FakeScrapeService : IScrapeService
    {
        public Action<IProgress<string>> OnRun { get; set; } = _ => { };

        public Task RunScraperAsync(IProgress<string> progress)
        {
            OnRun(progress);

            return Task.CompletedTask;
        }
    }
}
