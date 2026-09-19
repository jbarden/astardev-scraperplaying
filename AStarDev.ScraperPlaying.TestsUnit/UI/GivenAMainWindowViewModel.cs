using System.Text.Json;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Operations;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.UI;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.ScraperPlaying.TestsUnit.UI;

public sealed class GivenAMainWindowViewModel : IDisposable
{
    private readonly IScrapeService scrapeService = Substitute.For<IScrapeService>();
    private readonly OperationCoordinator operationCoordinator = new();
    private readonly MainWindowViewModel viewModel;
    private int changedCount;

    public GivenAMainWindowViewModel()
    {
        scrapeService.ValidateRootDirectoriesAsync().Returns(Problems());
        viewModel = new(scrapeService, operationCoordinator, new OperationRunner(operationCoordinator, NullLogger<OperationRunner>.Instance));
        viewModel.Changed += (_, _) => changedCount++;
    }

    private static IReadOnlyList<string> Problems(params string[] problems) => problems;

    public void Dispose()
    {
        viewModel.Dispose();
        operationCoordinator.Dispose();
    }

    [Fact]
    public void when_created_then_it_is_ready_and_the_scraper_can_run()
    {
        viewModel.StatusText.ShouldBe("Ready for action.");
        viewModel.IsRunEnabled.ShouldBeTrue();
        viewModel.IsCancelEnabled.ShouldBeFalse();
        viewModel.AreConfigurationOperationsEnabled.ShouldBeTrue();
    }

    [Fact]
    public void when_a_status_message_is_appended_then_the_status_text_shows_the_log_and_a_change_is_raised()
    {
        viewModel.AppendStatusMessage("first");
        viewModel.AppendStatusMessage("second");

        viewModel.StatusText.ShouldBe($"first{Environment.NewLine}second");
        changedCount.ShouldBe(2);
    }

    [Fact]
    public void when_an_operation_is_running_then_run_and_configuration_operations_are_disabled_and_cancel_is_enabled()
    {
        operationCoordinator.TryStart(out _);

        viewModel.IsRunEnabled.ShouldBeFalse();
        viewModel.AreConfigurationOperationsEnabled.ShouldBeFalse();
        viewModel.IsCancelEnabled.ShouldBeTrue();
        changedCount.ShouldBe(1);
    }

    [Fact]
    public void when_cancel_is_requested_then_the_running_operation_is_cancelled()
    {
        operationCoordinator.TryStart(out var cancellationToken);

        viewModel.CancelOperation();

        cancellationToken.IsCancellationRequested.ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_root_directory_exists_then_the_scraper_can_run()
    {
        await viewModel.CheckRootDirectoryAvailabilityAsync();

        viewModel.IsRunEnabled.ShouldBeTrue();
        viewModel.StatusText.ShouldBe("Ready for action.");
    }

    [Fact]
    public async Task when_the_root_directory_is_missing_then_it_is_reported_and_the_scraper_cannot_run()
    {
        scrapeService.ValidateRootDirectoriesAsync().Returns(Problems("Root directory could not be found."));

        await viewModel.CheckRootDirectoryAvailabilityAsync();

        viewModel.IsRunEnabled.ShouldBeFalse();
        viewModel.StatusText.ShouldBe("Root directory could not be found.");
    }

    [Fact]
    public async Task when_several_root_directory_problems_exist_then_each_is_reported_and_the_scraper_cannot_run()
    {
        scrapeService.ValidateRootDirectoriesAsync().Returns(Problems("Root directory could not be found.", "Famous root directory is not configured."));

        await viewModel.CheckRootDirectoryAvailabilityAsync();

        viewModel.IsRunEnabled.ShouldBeFalse();
        viewModel.StatusText.ShouldBe($"Root directory could not be found.{Environment.NewLine}Famous root directory is not configured.");
    }

    [Fact]
    public async Task when_the_scraper_is_run_then_its_progress_is_shown_in_the_status_text()
    {
        var progressShown = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        viewModel.Changed += (_, _) => progressShown.TrySetResult();
        scrapeService.RunScraperAsync(Arg.Any<IProgress<string>>()).Returns(call =>
        {
            call.Arg<IProgress<string>>().Report("scraping");

            return Task.CompletedTask;
        });

        await viewModel.RunScraperAsync();

        await progressShown.Task.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        viewModel.StatusText.ShouldContain("scraping");
    }

    [Fact]
    public async Task when_an_import_completes_then_it_is_reported_and_the_operation_is_completed()
    {
        await viewModel.ImportConfigurationAsync(_ => Task.FromResult(Option.Some(Unit.Instance)));

        viewModel.StatusText.ShouldBe("Scrape configuration imported.");
        operationCoordinator.IsOperationRunning.ShouldBeFalse();
    }

    [Fact]
    public async Task when_an_import_is_dismissed_then_it_is_reported_as_not_completed()
    {
        await viewModel.ImportConfigurationAsync(_ => Task.FromResult(Option.None<Unit>()));

        viewModel.StatusText.ShouldBe("Scrape configuration import could not be completed.");
    }

    [Fact]
    public async Task when_an_import_is_cancelled_then_it_is_reported_and_the_operation_is_completed()
    {
        await viewModel.ImportConfigurationAsync(cancellationToken =>
        {
            operationCoordinator.Cancel();

            throw new OperationCanceledException(cancellationToken);
        });

        viewModel.StatusText.ShouldBe("Scrape configuration import cancelled.");
        operationCoordinator.IsOperationRunning.ShouldBeFalse();
    }

    [Theory]
    [InlineData(typeof(IOException))]
    [InlineData(typeof(JsonException))]
    [InlineData(typeof(InvalidOperationException))]
    public async Task when_an_import_fails_with_an_expected_error_then_it_is_reported_and_the_operation_is_completed(Type exceptionType)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType, "boom")!;

        await viewModel.ImportConfigurationAsync(_ => throw exception);

        viewModel.StatusText.ShouldBe("Unable to import scrape configuration. boom");
        operationCoordinator.IsOperationRunning.ShouldBeFalse();
    }

    [Fact]
    public async Task when_an_import_fails_with_an_unexpected_error_then_it_propagates_and_the_operation_is_still_completed()
    {
        await Should.ThrowAsync<NotSupportedException>(() => viewModel.ImportConfigurationAsync(_ => throw new NotSupportedException("boom")));

        operationCoordinator.IsOperationRunning.ShouldBeFalse();
    }

    [Fact]
    public async Task when_another_operation_is_running_then_an_import_does_nothing()
    {
        operationCoordinator.TryStart(out _);
        var ran = false;

        await viewModel.ImportConfigurationAsync(_ =>
        {
            ran = true;

            return Task.FromResult(Option.Some(Unit.Instance));
        });

        ran.ShouldBeFalse();
        operationCoordinator.IsOperationRunning.ShouldBeTrue();
    }

    [Theory]
    [InlineData(true, "Scrape configuration exported.")]
    [InlineData(false, "No scrape configuration was found to export.")]
    public async Task when_an_export_completes_then_the_outcome_is_reported(bool exported, string expected)
    {
        await viewModel.ExportConfigurationAsync(_ => Task.FromResult(Option.Some(exported)));

        viewModel.StatusText.ShouldBe(expected);
    }

    [Fact]
    public async Task when_an_export_is_dismissed_then_it_is_reported_as_not_completed()
    {
        await viewModel.ExportConfigurationAsync(_ => Task.FromResult(Option.None<bool>()));

        viewModel.StatusText.ShouldBe("Scrape configuration export could not be completed.");
    }

    [Fact]
    public async Task when_an_export_fails_with_an_expected_error_then_it_is_reported()
    {
        await viewModel.ExportConfigurationAsync(_ => throw new IOException("disk full"));

        viewModel.StatusText.ShouldBe("Unable to export scrape configuration. disk full");
    }

    [Fact]
    public async Task when_an_export_fails_with_a_json_error_then_it_is_not_treated_as_expected()
    {
        await Should.ThrowAsync<JsonException>(() => viewModel.ExportConfigurationAsync(_ => throw new JsonException("bad")));

        operationCoordinator.IsOperationRunning.ShouldBeFalse();
    }

    [Fact]
    public void when_disposed_then_operation_state_changes_no_longer_raise_changes()
    {
        viewModel.Dispose();

        operationCoordinator.TryStart(out _);

        changedCount.ShouldBe(0);
    }
}
