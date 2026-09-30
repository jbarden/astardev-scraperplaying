using System.Text.Json;
using AStarDev.ScraperPlaying.UI;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.ScraperPlaying.TestsUnit.UI;

public sealed class GivenAUserOperationRunner : IDisposable
{
    private readonly OperationCoordinator coordinator = new();
    private readonly StatusReporter status = new(NullLogger<StatusReporter>.Instance);
    private readonly UserOperationRunner runner;

    public GivenAUserOperationRunner() => runner = new(coordinator, status);

    public void Dispose() => coordinator.Dispose();

    [Fact]
    public async Task when_the_operation_succeeds_then_it_runs_with_the_operation_marked_as_running_and_completes_afterwards()
    {
        var wasRunning = false;

        await runner.RunAsync("cancelled", "failed", _ =>
        {
            wasRunning = coordinator.IsOperationRunning;

            return Task.CompletedTask;
        });

        (wasRunning, coordinator.IsOperationRunning, status.Text).ShouldBe((true, false, string.Empty));
    }

    [Fact]
    public async Task when_another_operation_is_already_running_then_the_operation_is_not_run()
    {
        _ = coordinator.TryStart(out _);
        var ran = false;

        await runner.RunAsync("cancelled", "failed", _ =>
        {
            ran = true;

            return Task.CompletedTask;
        });

        ran.ShouldBeFalse();
    }

    [Fact]
    public async Task when_the_operation_is_cancelled_then_the_cancelled_message_is_reported_and_the_operation_completes()
    {
        await runner.RunAsync("Import cancelled.", "failed", async cancellationToken =>
        {
            runner.Cancel();
            await Task.Delay(Timeout.Infinite, cancellationToken);
        });

        (status.Text, coordinator.IsOperationRunning).ShouldBe(("Import cancelled.", false));
    }

    [Theory]
    [MemberData(nameof(ExpectedFailures))]
    public async Task when_the_operation_fails_with_an_expected_exception_then_the_failure_is_reported_and_the_operation_completes(Exception failure)
    {
        await runner.RunAsync("cancelled", "Unable to import.", _ => throw failure);

        (status.Text, coordinator.IsOperationRunning).ShouldBe(($"Unable to import. {failure.Message}", false));
    }

    [Fact]
    public async Task when_the_operation_fails_with_an_unexpected_exception_then_it_propagates_and_the_operation_still_completes()
    {
        _ = await Should.ThrowAsync<ArgumentException>(() => runner.RunAsync("cancelled", "failed", _ => throw new ArgumentException("unexpected")));

        coordinator.IsOperationRunning.ShouldBeFalse();
    }

    public static TheoryData<Exception> ExpectedFailures =>
    [
        new IOException("disk"),
        new JsonException("json"),
        new InvalidOperationException("invalid")
    ];
}
