namespace AStarDev.FunctionalParadigm.TestsUnit;

public sealed class GivenExceptionalTapError
{
    [Fact]
    public void when_tap_error_is_called_on_a_failure_then_the_handler_receives_the_exception_and_the_same_instance_is_returned()
    {
        var exception = new InvalidOperationException("boom");
        var exceptional = Exceptional.Failure<int>(exception);
        Exception? captured = null;

        var actual = exceptional.TapError(ex => captured = ex);

        captured.ShouldBeSameAs(exception);
        actual.ShouldBeSameAs(exceptional);
    }

    [Fact]
    public void when_tap_error_is_called_on_a_success_then_the_handler_is_not_invoked_and_the_same_instance_is_returned()
    {
        var exceptional = Exceptional.Success(4);
        bool handlerInvoked = false;

        var actual = exceptional.TapError(_ => handlerInvoked = true);

        handlerInvoked.ShouldBeFalse();
        actual.ShouldBeSameAs(exceptional);
    }

    [Fact]
    public void when_tap_is_called_on_a_failure_without_a_failure_handler_then_the_same_instance_is_returned()
    {
        var exceptional = Exceptional.Failure<int>(new InvalidOperationException("boom"));
        bool successInvoked = false;

        var actual = exceptional.Tap(_ => successInvoked = true);

        successInvoked.ShouldBeFalse();
        actual.ShouldBeSameAs(exceptional);
    }

    [Fact]
    public async Task when_tap_error_async_is_called_on_a_failed_task_then_the_handler_receives_the_exception_and_the_result_is_unchanged()
    {
        var exception = new InvalidOperationException("boom");
        var exceptional = Exceptional.Failure<int>(exception);
        Exception? captured = null;

        var actual = await Task.FromResult(exceptional).TapErrorAsync(ex => captured = ex);

        captured.ShouldBeSameAs(exception);
        actual.ShouldBeSameAs(exceptional);
    }

    [Fact]
    public async Task when_tap_error_async_is_called_on_a_successful_task_then_the_handler_is_not_invoked()
    {
        var exceptional = Exceptional.Success(4);
        bool handlerInvoked = false;

        var actual = await Task.FromResult(exceptional).TapErrorAsync(_ => handlerInvoked = true);

        handlerInvoked.ShouldBeFalse();
        actual.ShouldBeSameAs(exceptional);
    }
}
