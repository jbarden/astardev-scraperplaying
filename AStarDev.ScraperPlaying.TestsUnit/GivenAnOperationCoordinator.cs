using AStarDev.ScraperPlaying.Home;

namespace AStarDev.ScraperPlaying.TestsUnit;

public sealed class GivenAnOperationCoordinator
{
    [Fact]
    public void when_an_operation_starts_then_another_cannot_start_until_it_completes()
    {
        var coordinator = new OperationCoordinator();

        coordinator.TryStart(out _).ShouldBeTrue();
        coordinator.IsOperationRunning.ShouldBeTrue();
        coordinator.TryStart(out _).ShouldBeFalse();

        coordinator.Complete();

        coordinator.IsOperationRunning.ShouldBeFalse();
        coordinator.TryStart(out _).ShouldBeTrue();
    }

    [Fact]
    public void when_the_current_operation_is_cancelled_then_its_token_is_cancelled()
    {
        var coordinator = new OperationCoordinator();
        coordinator.TryStart(out var cancellationToken);

        coordinator.Cancel();

        cancellationToken.IsCancellationRequested.ShouldBeTrue();
    }

    [Fact]
    public void when_a_cancelled_operation_completes_then_the_next_operation_receives_a_fresh_token()
    {
        var coordinator = new OperationCoordinator();
        coordinator.TryStart(out var cancelledToken);
        coordinator.Cancel();
        coordinator.Complete();

        coordinator.TryStart(out var nextToken).ShouldBeTrue();

        cancelledToken.IsCancellationRequested.ShouldBeTrue();
        nextToken.IsCancellationRequested.ShouldBeFalse();
    }

    [Fact]
    public void when_an_operation_starts_and_completes_then_state_changes_are_reported()
    {
        var coordinator = new OperationCoordinator();
        var stateChangeCount = 0;
        coordinator.StateChanged += (_, _) => stateChangeCount++;

        coordinator.TryStart(out _);
        coordinator.Complete();

        stateChangeCount.ShouldBe(2);
    }

    [Fact]
    public void when_disposed_multiple_times_then_no_exception_is_thrown()
    {
        var coordinator = new OperationCoordinator();

        Should.NotThrow(coordinator.Dispose);
        Should.NotThrow(coordinator.Dispose);
    }
}