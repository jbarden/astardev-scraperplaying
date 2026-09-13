namespace AStarDev.FunctionalParadigm.TestsUnit;

public sealed class GivenUnit
{
    [Fact]
    public void when_unit_values_are_compared_then_they_are_all_equal()
    {
        var a = UnitFp.Instance;
        var b = new UnitFp();

        a.ShouldBe(b);
        UnitFp.Instance.ShouldBe(a);
    }
}
