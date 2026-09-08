using Shouldly;
using Xunit;
using static AStarDev.FunctionalParadigm.Option<string>;

namespace AStarDev.FunctionalParadigm.TestsUnit;

public sealed class GivenAnOptionExtensions
{
    [Fact]
    public async Task when_match_async_some_task_then_the_async_some_function_is_called()
    {
        var optionTask = Task.FromResult<Option<string>>("value");

        var result = await optionTask.MatchAsync(value => Task.FromResult(value.Length), () => -1);

        result.ShouldBe(5);
    }

    [Fact]
    public async Task when_match_async_none_task_then_the_none_function_is_called()
    {
        var optionTask = Task.FromResult<Option<string>>(None.Instance);

        var result = await optionTask.MatchAsync(_ => Task.FromResult(-1), () => 42);

        result.ShouldBe(42);
    }

    [Fact]
    public async Task when_match_async_void_some_task_then_the_async_some_action_is_called()
    {
        var optionTask = Task.FromResult<Option<string>>("value");
        var captured = string.Empty;

        await optionTask.MatchAsync(value =>
        {
            captured = value;
            return Task.CompletedTask;
        }, () => captured = "none");

        captured.ShouldBe("value");
    }

    [Fact]
    public async Task when_match_async_void_none_task_then_the_none_action_is_called()
    {
        var optionTask = Task.FromResult<Option<string>>(None.Instance);
        var captured = string.Empty;

        await optionTask.MatchAsync(_ => Task.CompletedTask, () => captured = "none");

        captured.ShouldBe("none");
    }
}
