namespace AStarDev.Utilities.TestsUnit;

public class GivenTimeSpanExtensions
{
    [Fact]
    public void when_the_elapsed_is_less_than_two_minutes_then_is_just_now_returns_true()
        => TimeSpan.FromMinutes(1).IsJustNow().ShouldBeTrue();

    [Fact]
    public void when_the_elapsed_is_more_than_two_minutes_then_is_just_now_returns_false()
        => TimeSpan.FromMinutes(3).IsJustNow().ShouldBeFalse();

    [Fact]
    public void when_the_elapsed_is_less_than_one_hour_then_is_minutes_ago_returns_true()
        => TimeSpan.FromMinutes(30).IsMinutesAgo().ShouldBeTrue();

    [Fact]
    public void when_the_elapsed_is_more_than_one_hour_then_is_minutes_ago_returns_false()
        => TimeSpan.FromHours(2).IsMinutesAgo().ShouldBeFalse();

    [Fact]
    public void when_the_elapsed_is_less_than_one_day_then_is_hours_ago_returns_true()
        => TimeSpan.FromHours(12).IsHoursAgo().ShouldBeTrue();

    [Fact]
    public void when_the_elapsed_is_more_than_one_day_then_is_hours_ago_returns_false()
        => TimeSpan.FromDays(2).IsHoursAgo().ShouldBeFalse();

    [Fact]
    public void when_the_elapsed_is_less_than_two_days_then_is_yesterday_returns_true()
        => TimeSpan.FromDays(1).IsYesterday().ShouldBeTrue();

    [Fact]
    public void when_the_elapsed_is_more_than_two_days_then_is_yesterday_returns_false()
        => TimeSpan.FromDays(3).IsYesterday().ShouldBeFalse();

    [Fact]
    public void when_the_elapsed_is_one_second_then_to_duration_string_uses_singular_seconds()
        => TimeSpan.FromSeconds(1).ToDurationString().ShouldBe("1 second");

    [Fact]
    public void when_the_elapsed_is_under_a_minute_then_to_duration_string_formats_in_seconds()
        => TimeSpan.FromSeconds(42).ToDurationString().ShouldBe("42 seconds");

    [Fact]
    public void when_the_elapsed_is_exactly_sixty_seconds_then_to_duration_string_formats_in_minutes()
        => TimeSpan.FromSeconds(60).ToDurationString().ShouldBe("1 minute");

    [Fact]
    public void when_the_elapsed_is_under_an_hour_then_to_duration_string_formats_in_minutes()
        => TimeSpan.FromMinutes(42).ToDurationString().ShouldBe("42 minutes");

    [Fact]
    public void when_the_elapsed_is_exactly_sixty_minutes_then_to_duration_string_formats_in_hours()
        => TimeSpan.FromMinutes(60).ToDurationString().ShouldBe("1 hour");

    [Fact]
    public void when_the_elapsed_is_over_an_hour_then_to_duration_string_formats_in_hours()
        => TimeSpan.FromHours(2.5).ToDurationString().ShouldBe("2.5 hours");
}
