namespace AStarDev.FunctionalParadigm.TestsUnit;

public sealed class GivenExceptionalTaskMatchAndToResult
{
    [Fact]
    public async Task when_match_async_with_a_task_returning_success_handler_is_called_on_a_successful_task_then_the_success_handler_is_awaited()
    {
        int captured = 0;
        bool failureInvoked = false;

        await Task.FromResult(Exceptional.Success(4)).MatchAsync(
            value =>
            {
                captured = value;

                return Task.CompletedTask;
            },
            _ =>
            {
                failureInvoked = true;

                return 0;
            });

        captured.ShouldBe(4);
        failureInvoked.ShouldBeFalse();
    }

    [Fact]
    public async Task when_match_async_with_a_task_returning_success_handler_is_called_on_a_failed_task_then_the_failure_handler_receives_the_exception()
    {
        var exception = new InvalidOperationException("boom");
        Exception? captured = null;
        bool successInvoked = false;

        await Task.FromResult(Exceptional.Failure<int>(exception)).MatchAsync(
            _ =>
            {
                successInvoked = true;

                return Task.CompletedTask;
            },
            ex =>
            {
                captured = ex;

                return 0;
            });

        captured.ShouldBeSameAs(exception);
        successInvoked.ShouldBeFalse();
    }

    [Fact]
    public async Task when_to_result_async_is_called_on_a_successful_task_then_returns_an_ok_result()
    {
        var actual = await Task.FromResult(Exceptional.Success(4)).ToResultAsync(ex => ex.Message);

        actual.ShouldBe(new Ok<int, string>(4));
    }

    [Fact]
    public async Task when_to_result_async_is_called_on_a_failed_task_then_returns_a_fail_result_with_the_mapped_error()
    {
        var actual = await Task.FromResult(Exceptional.Failure<int>(new InvalidOperationException("boom"))).ToResultAsync(ex => ex.Message);

        actual.ShouldBe(new Fail<int, string>("boom"));
    }
}
