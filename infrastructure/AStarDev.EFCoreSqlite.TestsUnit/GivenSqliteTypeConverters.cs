using AStarDev.FunctionalParadigm;
using Shouldly;
using Xunit;

namespace AStarDev.EFCoreSqlite.TestsUnit;

public sealed class GivenSqliteTypeConverters
{
    [Fact]
    public void when_a_date_time_offset_is_converted_then_utc_ticks_are_stored()
    {
        var value = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.FromHours(2));

        SqliteTypeConverters.DateTimeOffsetToTicks.ConvertToProvider(value)
            .ShouldBe(value.ToUniversalTime().Ticks);
    }

    [Fact]
    public void when_a_nullable_date_time_offset_is_empty_then_null_is_stored()
    {
        SqliteTypeConverters.NullableDateTimeOffsetToTicks.ConvertToProvider(null).ShouldBeNull();
        SqliteTypeConverters.NullableDateTimeOffsetToTicks.ConvertFromProvider(null).ShouldBeNull();
    }

    [Fact]
    public void when_a_time_span_is_converted_then_ticks_round_trip()
    {
        var value = TimeSpan.FromMinutes(90);

        SqliteTypeConverters.TimeSpanToTicks.ConvertFromProvider(
            SqliteTypeConverters.TimeSpanToTicks.ConvertToProvider(value)).ShouldBe(value);
    }

    [Fact]
    public void when_a_guid_is_converted_then_bytes_round_trip()
    {
        var value = Guid.NewGuid();

        SqliteTypeConverters.GuidToBytes.ConvertFromProvider(
            SqliteTypeConverters.GuidToBytes.ConvertToProvider(value)).ShouldBe(value);
    }

    [Fact]
    public void when_a_decimal_is_converted_then_cents_are_stored()
    {
        SqliteTypeConverters.DecimalToCents.ConvertToProvider(12.34m).ShouldBe(1234L);
        SqliteTypeConverters.DecimalToCents.ConvertFromProvider(1234L).ShouldBe(12.34m);
    }

    [Fact]
    public void when_an_option_is_present_then_its_value_is_stored()
    {
        Option<string> option = "value";
        var convertFromProvider = SqliteTypeConverters.OptionStringToNullableString.ConvertFromProviderExpression.Compile();

        SqliteTypeConverters.OptionStringToNullableString.ConvertToProvider(option).ShouldBe("value");
        ((Option<string>)convertFromProvider("value")!)
            .Match(value => value, () => "missing").ShouldBe("value");
    }

    [Fact]
    public void when_an_option_is_empty_then_null_is_stored()
    {
        SqliteTypeConverters.OptionStringToNullableString
            .ConvertToProvider(Option<string>.None.Instance).ShouldBeNull();
    }
}