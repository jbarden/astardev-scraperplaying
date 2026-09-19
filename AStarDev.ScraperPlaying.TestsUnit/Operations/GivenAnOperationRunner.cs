using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Operations;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.ScraperPlaying.TestsUnit.Operations;

public sealed class GivenAnOperationRunner : IDisposable
{
    private readonly OperationCoordinator coordinator = new();
    private readonly OperationRunner runner;
    private readonly List<string> reported = [];
    private readonly OperationReporting reporting;

    public GivenAnOperationRunner()
    {
        runner = new(coordinator, NullLogger<OperationRunner>.Instance);
        reporting = new(reported.Add, "cancelled", exception => exception is IOException ? Option.Some($"expected: {exception.Message}") : Option.None<string>());
    }

    public void Dispose() => coordinator.Dispose();

    [Fact]
    public async Task when_work_runs_then_the_operation_is_running_during_it_and_completed_after_it()
    {
        var wasRunning = false;

        await runner.RunAsync(_ =>
        {
            wasRunning = coordinator.IsOperationRunning;

            return Task.CompletedTask;
        }, reporting);

        wasRunning.ShouldBeTrue();
        coordinator.IsOperationRunning.ShouldBeFalse();
        reported.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_another_operation_is_running_then_the_work_does_not_run_and_that_operation_keeps_running()
    {
        coordinator.TryStart(out _);
        var ran = false;

        await runner.RunAsync(_ =>
        {
            ran = true;

            return Task.CompletedTask;
        }, reporting);

        ran.ShouldBeFalse();
        coordinator.IsOperationRunning.ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_work_is_cancelled_then_the_cancelled_message_is_reported_and_the_operation_completed()
    {
        await runner.RunAsync(cancellationToken =>
        {
            coordinator.Cancel();

            throw new OperationCanceledException(cancellationToken);
        }, reporting);

        reported.ShouldBe(["cancelled"]);
        coordinator.IsOperationRunning.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_work_fails_with_an_expected_error_then_it_is_reported_and_the_operation_completed()
    {
        await runner.RunAsync(_ => throw new IOException("disk full"), reporting);

        reported.ShouldBe(["expected: disk full"]);
        coordinator.IsOperationRunning.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_work_fails_with_an_unexpected_error_then_it_propagates_and_the_operation_is_still_completed()
    {
        await Should.ThrowAsync<NotSupportedException>(() => runner.RunAsync(_ => throw new NotSupportedException("boom"), reporting));

        reported.ShouldBeEmpty();
        coordinator.IsOperationRunning.ShouldBeFalse();
    }

    [Fact]
    public async Task when_an_operation_cancellation_was_not_requested_then_it_is_not_treated_as_a_user_cancel()
    {
        await Should.ThrowAsync<OperationCanceledException>(() => runner.RunAsync(_ => throw new OperationCanceledException(), reporting));

        reported.ShouldBeEmpty();
    }
}
