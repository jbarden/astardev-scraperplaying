using Shouldly;
using Xunit;
using static AStarDev.FunctionalParadigm.Option<string>;

namespace AStarDev.FunctionalParadigm.TestsUnit;

public sealed class GivenAnOption
{
    [Fact]
    public void when_a_value_is_implicitly_converted_then_it_is_some()
    {
        Option<string> option = "value";

        option.ShouldBeOfType<Some>().Value.ShouldBe("value");
    }

    [Fact]
    public void when_matching_some_then_the_some_function_is_called()
    {
        Option<string> option = "value";

        option.Match(value => value.Length, () => -1).ShouldBe(5);
    }

    [Fact]
    public void when_matching_none_then_the_none_function_is_called()
    {
        Option<string> option = None.Instance;

        option.Match(_ => -1, () => 42).ShouldBe(42);
    }

    [Fact]
    public void when_some_is_created_with_null_then_it_throws()
    {
        Should.Throw<ArgumentNullException>(() => new Some(null!));
    }

    [Fact]
    public void when_options_are_formatted_then_their_state_is_visible()
    {
        ((Option<string>)"value").ToString().ShouldBe("Some(value)");
        None.Instance.ToString().ShouldBe("None");
    }
}