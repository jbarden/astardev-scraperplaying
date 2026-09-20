namespace AStarDev.FunctionalParadigm.TestsUnit;

public sealed class GivenResultTapWithoutFailureHandler
{
    [Fact]
    public void when_tap_is_called_on_a_failure_without_a_failure_handler_then_the_success_handler_is_not_invoked_and_the_same_instance_is_returned()
    {
        var result = Result.Failure<int, string>("failed");
        bool successInvoked = false;

        var actual = result.Tap(_ => successInvoked = true);

        successInvoked.ShouldBeFalse();
        actual.ShouldBeSameAs(result);
    }
}
