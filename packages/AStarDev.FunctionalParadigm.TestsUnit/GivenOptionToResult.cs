namespace AStarDev.FunctionalParadigm.TestsUnit;

public sealed class GivenOptionToResult
{
    [Fact]
    public void when_to_result_is_called_on_some_then_returns_ok_and_does_not_invoke_the_error_factory()
    {
        bool factoryInvoked = false;

        var actual = Option.Some(4).ToResult(() =>
        {
            factoryInvoked = true;

            return "missing";
        });

        actual.ShouldBe(new Ok<int, string>(4));
        factoryInvoked.ShouldBeFalse();
    }

    [Fact]
    public void when_to_result_is_called_on_none_then_returns_fail_with_the_factory_error() =>
        Option.None<int>().ToResult(() => "missing").ShouldBe(new Fail<int, string>("missing"));

    [Fact]
    public async Task when_to_result_async_is_called_on_some_then_returns_ok_and_does_not_invoke_the_async_error_factory()
    {
        bool factoryInvoked = false;

        var actual = await Option.Some(4).ToResultAsync(() =>
        {
            factoryInvoked = true;

            return Task.FromResult("missing");
        });

        actual.ShouldBe(new Ok<int, string>(4));
        factoryInvoked.ShouldBeFalse();
    }

    [Fact]
    public async Task when_to_result_async_is_called_on_none_then_returns_fail_with_the_awaited_factory_error() =>
        (await Option.None<int>().ToResultAsync(() => Task.FromResult("missing"))).ShouldBe(new Fail<int, string>("missing"));

    [Fact]
    public async Task when_to_result_async_is_called_on_a_task_of_some_with_a_sync_factory_then_returns_ok()
    {
        Func<string> factory = () => "missing";

        var actual = await Task.FromResult(Option.Some(4)).ToResultAsync(factory);

        actual.ShouldBe(new Ok<int, string>(4));
    }

    [Fact]
    public async Task when_to_result_async_is_called_on_a_task_of_none_with_a_sync_factory_then_returns_fail()
    {
        Func<string> factory = () => "missing";

        var actual = await Task.FromResult(Option.None<int>()).ToResultAsync(factory);

        actual.ShouldBe(new Fail<int, string>("missing"));
    }

    [Fact]
    public async Task when_to_result_async_is_called_on_a_task_of_some_with_an_async_factory_then_returns_ok()
    {
        Func<Task<string>> factory = () => Task.FromResult("missing");

        var actual = await Task.FromResult(Option.Some(4)).ToResultAsync(factory);

        actual.ShouldBe(new Ok<int, string>(4));
    }

    [Fact]
    public async Task when_to_result_async_is_called_on_a_task_of_none_with_an_async_factory_then_returns_fail()
    {
        Func<Task<string>> factory = () => Task.FromResult("missing");

        var actual = await Task.FromResult(Option.None<int>()).ToResultAsync(factory);

        actual.ShouldBe(new Fail<int, string>("missing"));
    }
}
