namespace AStarDev.FunctionalParadigm.TestsUnit;

public sealed class GivenOptionMatchAsyncForSideEffects
{
    [Fact]
    public async Task when_match_async_is_called_on_some_then_the_async_some_handler_is_awaited_and_none_is_not_invoked()
    {
        int captured = 0;
        bool noneInvoked = false;

        await Option.Some(4).MatchAsync(
            value =>
            {
                captured = value;

                return Task.CompletedTask;
            },
            () => noneInvoked = true);

        captured.ShouldBe(4);
        noneInvoked.ShouldBeFalse();
    }

    [Fact]
    public async Task when_match_async_is_called_on_none_then_the_none_action_runs_and_some_is_not_invoked()
    {
        bool someInvoked = false;
        bool noneInvoked = false;

        await Option.None<int>().MatchAsync(
            _ =>
            {
                someInvoked = true;

                return Task.CompletedTask;
            },
            () => noneInvoked = true);

        someInvoked.ShouldBeFalse();
        noneInvoked.ShouldBeTrue();
    }

    [Fact]
    public async Task when_match_async_is_called_on_a_task_of_some_with_async_some_and_sync_none_returning_values_then_returns_the_some_value()
    {
        int actual = await Task.FromResult(Option.Some(4)).MatchAsync(value => Task.FromResult(value * 2), () => -1);

        actual.ShouldBe(8);
    }

    [Fact]
    public async Task when_match_async_is_called_on_a_task_of_none_with_async_some_and_sync_none_returning_values_then_returns_the_none_value()
    {
        int actual = await Task.FromResult(Option.None<int>()).MatchAsync(value => Task.FromResult(value * 2), () => -1);

        actual.ShouldBe(-1);
    }

    [Fact]
    public async Task when_match_async_is_called_on_a_task_of_some_for_side_effects_then_the_async_some_handler_is_awaited()
    {
        int captured = 0;
        bool noneInvoked = false;

        await Task.FromResult(Option.Some(4)).MatchAsync(
            value =>
            {
                captured = value;

                return Task.CompletedTask;
            },
            () => noneInvoked = true);

        captured.ShouldBe(4);
        noneInvoked.ShouldBeFalse();
    }

    [Fact]
    public async Task when_match_async_is_called_on_a_task_of_none_for_side_effects_then_the_none_action_runs()
    {
        bool someInvoked = false;
        bool noneInvoked = false;

        await Task.FromResult(Option.None<int>()).MatchAsync(
            _ =>
            {
                someInvoked = true;

                return Task.CompletedTask;
            },
            () => noneInvoked = true);

        someInvoked.ShouldBeFalse();
        noneInvoked.ShouldBeTrue();
    }
}
