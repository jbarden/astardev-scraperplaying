namespace AStarDev.Utilities.TestsUnit;

public class GivenExceptionExtensions
{
    [Fact]
    public void when_the_exception_has_no_inner_exception_then_the_message_chain_is_its_message()
        => new InvalidOperationException("outer").ToMessageChain().ShouldBe("outer");

    [Fact]
    public void when_the_exception_has_an_inner_exception_then_the_message_chain_lists_both_outermost_first()
        => new InvalidOperationException("outer", new IOException("inner")).ToMessageChain().ShouldBe("outer Caused by: inner");

    [Fact]
    public void when_the_exception_has_several_nested_inner_exceptions_then_the_message_chain_lists_every_one()
        => new InvalidOperationException("one", new IOException("two", new ArgumentException("three"))).ToMessageChain().ShouldBe("one Caused by: two Caused by: three");

    [Fact]
    public void when_an_inner_exception_repeats_the_message_above_it_then_it_is_not_repeated()
        => new InvalidOperationException("same", new IOException("same")).ToMessageChain().ShouldBe("same");

    [Fact]
    public void when_an_inner_exception_has_a_blank_message_then_it_is_skipped()
        => new InvalidOperationException("outer", new IOException(" ", new ArgumentException("root"))).ToMessageChain().ShouldBe("outer Caused by: root");
}
