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
    public void when_some_is_created_with_null_then_it_throws() => Should.Throw<ArgumentNullException>(() => new Some(null!));

    [Fact]
    public async Task when_match_async_some_then_the_async_some_function_is_called()
    {
        Option<string> option = "value";

        var result = await option.MatchAsync(value => Task.FromResult(value.Length), () => -1);

        result.ShouldBe(5);
    }

    [Fact]
    public async Task when_match_async_none_then_the_none_function_is_called()
    {
        Option<string> option = None.Instance;

        var result = await option.MatchAsync(_ => Task.FromResult(-1), () => 42);

        result.ShouldBe(42);
    }

    [Fact]
    public async Task when_match_async_void_some_then_the_async_some_action_is_called()
    {
        Option<string> option = "value";
        var captured = string.Empty;

        await option.MatchAsync(value =>
        {
            captured = value;
            return Task.CompletedTask;
        }, () => captured = "none");

        captured.ShouldBe("value");
    }

    [Fact]
    public async Task when_match_async_void_none_then_the_none_action_is_called()
    {
        Option<string> option = None.Instance;
        var captured = string.Empty;

        await option.MatchAsync(_ => Task.CompletedTask, () => captured = "none");

        captured.ShouldBe("none");
    }

    [Fact]
    public void when_options_are_formatted_then_their_state_is_visible()
    {
        ((Option<string>)"value").ToString().ShouldBe("Some(value)");
        None.Instance.ToString().ShouldBe("None");
    }
}