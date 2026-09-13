using Shouldly;
using Xunit;

namespace AStarDev.EFCoreSqlite.TestsUnit;

public sealed class GivenAStrongGuidIdValueGenerator
{
    private readonly record struct TestId(Guid Value);

    [Fact]
    public void when_next_is_called_then_a_non_empty_id_is_returned()
    {
        var generator = new StrongGuidIdValueGenerator<TestId>(value => new TestId(value));

        var generated = generator.Next(null!);

        generated.Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void when_next_is_called_repeatedly_then_distinct_ids_are_returned()
    {
        var generator = new StrongGuidIdValueGenerator<TestId>(value => new TestId(value));

        var first = generator.Next(null!);
        var second = generator.Next(null!);

        first.ShouldNotBe(second);
    }

    [Fact]
    public void when_checked_then_it_does_not_generate_temporary_values()
    {
        var generator = new StrongGuidIdValueGenerator<TestId>(value => new TestId(value));

        generator.GeneratesTemporaryValues.ShouldBeFalse();
    }
}
