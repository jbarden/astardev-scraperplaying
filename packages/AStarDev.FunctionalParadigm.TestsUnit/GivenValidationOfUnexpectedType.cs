namespace AStarDev.FunctionalParadigm.TestsUnit;

public sealed class GivenValidationOfUnexpectedType
{
    private sealed record Rogue<T> : Validation<T>;

    [Fact]
    public void when_match_is_called_on_an_unexpected_validation_type_then_throws_invalid_operation_exception() =>
        Should.Throw<InvalidOperationException>(() => new Rogue<int>().Match(value => value, _ => -1))
            .Message.ShouldBe("Unexpected validation type.");

    [Fact]
    public void when_to_result_is_called_on_an_unexpected_validation_type_then_throws_invalid_operation_exception() =>
        Should.Throw<InvalidOperationException>(() => new Rogue<int>().ToResult(errors => errors.Count))
            .Message.ShouldBe("Unexpected validation type.");

    [Fact]
    public void when_combine_is_called_with_an_unexpected_validation_type_then_throws_invalid_operation_exception()
    {
        Validation<int>[] validations = [Validation.Valid(1), new Rogue<int>()];

        Should.Throw<InvalidOperationException>(() => validations.Combine())
            .Message.ShouldBe("Unexpected validation type.");
    }

    [Fact]
    public void when_apply_is_called_with_an_unexpected_validation_type_then_throws_invalid_operation_exception()
    {
        Validation<Func<int, int>> function = Validation.Valid<Func<int, int>>(value => value + 1);

        Should.Throw<InvalidOperationException>(() => function.Apply(new Rogue<int>()))
            .Message.ShouldBe("Unexpected validation type.");
    }
}
