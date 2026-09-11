using AStarDev.ScraperPlaying.Home;

namespace AStarDev.ScraperPlaying.TestsUnit;

public sealed class GivenAnElapsedTimeToFormat
{
    [Fact]
    public void when_under_a_minute_then_it_is_formatted_in_seconds()
        => ScrapeService.FormatElapsedTime(TimeSpan.FromSeconds(42)).ShouldBe("42 seconds");

    [Fact]
    public void when_exactly_sixty_seconds_then_it_is_formatted_in_minutes()
        => ScrapeService.FormatElapsedTime(TimeSpan.FromSeconds(60)).ShouldBe("1 minutes");

    [Fact]
    public void when_under_an_hour_then_it_is_formatted_in_minutes()
        => ScrapeService.FormatElapsedTime(TimeSpan.FromMinutes(42)).ShouldBe("42 minutes");

    [Fact]
    public void when_exactly_sixty_minutes_then_it_is_formatted_in_hours()
        => ScrapeService.FormatElapsedTime(TimeSpan.FromMinutes(60)).ShouldBe("1 hours");

    [Fact]
    public void when_over_an_hour_then_it_is_formatted_in_hours()
        => ScrapeService.FormatElapsedTime(TimeSpan.FromHours(2.5)).ShouldBe("2.5 hours");
}
