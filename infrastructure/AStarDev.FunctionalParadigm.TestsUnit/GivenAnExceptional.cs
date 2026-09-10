using Shouldly;
using Xunit;

namespace AStarDev.FunctionalParadigm.TestsUnit;

public sealed class GivenAnExceptional
{
    [Fact]
    public void when_matching_success_then_the_success_function_is_called()
    {
        Exceptional<string> exceptional = new Success<string>("value");

        exceptional.Match(value => value.Length, _ => -1).ShouldBe(5);
    }

    [Fact]
    public void when_matching_failure_then_the_failure_function_is_called()
    {
        var exception = new InvalidOperationException("failure");
        Exceptional<string> exceptional = new Failure<string>(exception);

        exceptional.Match(_ => -1, failure => failure.Message.Length).ShouldBe(7);
    }

    [Fact]
    public async Task when_matching_async_success_then_the_async_success_function_is_called()
    {
        Exceptional<string> exceptional = new Success<string>("value");

        var result = await Task.FromResult(exceptional).MatchAsync(value => Task.FromResult(value.Length), _ => -1);

        result.ShouldBe(5);
    }

    [Fact]
    public async Task when_matching_async_failure_then_the_failure_function_is_called()
    {
        var exception = new InvalidOperationException("failure");
        Exceptional<string> exceptional = new Failure<string>(exception);

        var result = await Task.FromResult(exceptional).MatchAsync(_ => Task.FromResult(-1), failure => failure.Message.Length);

        result.ShouldBe(7);
    }

    [Fact]
    public async Task when_matching_async_for_side_effects_then_the_success_action_is_called()
    {
        Exceptional<string> exceptional = new Success<string>("value");
        var captured = string.Empty;

        await Task.FromResult(exceptional).MatchAsync<string>(value =>
        {
            captured = value;

            return Task.CompletedTask;
        }, _ => "failure");

        captured.ShouldBe("value");
    }

    [Fact]
    public async Task when_matching_async_with_synchronous_handlers_then_the_handlers_are_called()
    {
        Exceptional<string> exceptional = new Success<string>("value");

        var result = await Task.FromResult(exceptional).MatchAsync(value => value.Length, _ => -1);

        result.ShouldBe(5);
    }
}