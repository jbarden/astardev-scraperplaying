namespace AStarDev.FunctionalParadigm.TestsUnit;

public sealed class GivenGetOrThrow
{
    [Fact]
    public void when_exceptional_is_success_then_value_is_returned()
    {
        Exceptional<int> exceptional = new Success<int>(7);

        exceptional.GetOrThrow().ShouldBe(7);
    }

    [Fact]
    public void when_exceptional_is_success_with_default_value_then_default_is_returned_not_thrown()
    {
        Exceptional<string?> exceptional = new Success<string?>(null);

        exceptional.GetOrThrow().ShouldBeNull();
    }

    [Fact]
    public void when_exceptional_is_failure_then_captured_exception_is_thrown()
    {
        var exception = new InvalidOperationException("boom");
        Exceptional<int> exceptional = new Failure<int>(exception);

        var thrown = Should.Throw<InvalidOperationException>(() => exceptional.GetOrThrow());

        thrown.ShouldBeSameAs(exception);
    }
}
