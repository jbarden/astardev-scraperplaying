using Shouldly;
using Xunit;

namespace AStarDev.FunctionalParadigm.TestsUnit;

public sealed class GivenALinqExtensions
{
    [Fact]
    public void when_the_source_has_elements_then_first_or_none_returns_some_of_the_first_element()
    {
        var source = new[] { 1, 2, 3 };

        var result = source.FirstOrNone();

        result.Match(value => value, () => -1).ShouldBe(1);
    }

    [Fact]
    public void when_the_source_is_empty_then_first_or_none_returns_none()
    {
        var source = Array.Empty<int>();

        var result = source.FirstOrNone();

        result.Match(_ => false, () => true).ShouldBeTrue();
    }

    [Fact]
    public void when_the_source_has_a_matching_element_then_first_or_none_with_a_predicate_returns_some_of_that_element()
    {
        var source = new[] { 1, 2, 3, 4 };

        var result = source.FirstOrNone(value => value % 2 == 0);

        result.Match(value => value, () => -1).ShouldBe(2);
    }

    [Fact]
    public void when_no_element_matches_then_first_or_none_with_a_predicate_returns_none()
    {
        var source = new[] { 1, 3, 5 };

        var result = source.FirstOrNone(value => value % 2 == 0);

        result.Match(_ => false, () => true).ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_async_source_has_elements_then_first_or_none_async_returns_some_of_the_first_element()
    {
        var result = await AsyncSequence(1, 2, 3).FirstOrNoneAsync(TestContext.Current.CancellationToken);

        result.Match(value => value, () => -1).ShouldBe(1);
    }

    [Fact]
    public async Task when_the_async_source_is_empty_then_first_or_none_async_returns_none()
    {
        var result = await AsyncSequence<int>().FirstOrNoneAsync(TestContext.Current.CancellationToken);

        result.Match(_ => false, () => true).ShouldBeTrue();
    }

    [Fact]
    public async Task when_the_async_source_has_a_matching_element_then_first_or_none_async_with_a_predicate_returns_some_of_that_element()
    {
        var result = await AsyncSequence(1, 2, 3, 4).FirstOrNoneAsync(value => value % 2 == 0, TestContext.Current.CancellationToken);

        result.Match(value => value, () => -1).ShouldBe(2);
    }

    [Fact]
    public async Task when_no_async_element_matches_then_first_or_none_async_with_a_predicate_returns_none()
    {
        var result = await AsyncSequence(1, 3, 5).FirstOrNoneAsync(value => value % 2 == 0, TestContext.Current.CancellationToken);

        result.Match(_ => false, () => true).ShouldBeTrue();
    }

    private static async IAsyncEnumerable<T> AsyncSequence<T>(params T[] values)
    {
        foreach (var value in values)
        {
            await Task.Yield();

            yield return value;
        }
    }
}
