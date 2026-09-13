using AStarDev.ScraperPlaying.Home;

namespace AStarDev.ScraperPlaying.TestsUnit;

public sealed class GivenAStatusMessageLog
{
    [Fact]
    public void when_a_single_message_is_appended_then_text_is_that_message()
    {
        var log = new StatusMessageLog(maximumMessages: 100);

        log.Append("first message");

        log.Text.ShouldBe("first message");
    }

    [Fact]
    public void when_multiple_messages_are_appended_then_text_joins_them_with_newlines_oldest_first()
    {
        var log = new StatusMessageLog(maximumMessages: 100);

        log.Append("first message");
        log.Append("second message");
        log.Append("third message");

        log.Text.ShouldBe($"first message{Environment.NewLine}second message{Environment.NewLine}third message");
    }

    [Fact]
    public void when_appending_beyond_capacity_then_the_oldest_message_is_discarded()
    {
        var log = new StatusMessageLog(maximumMessages: 2);

        log.Append("first message");
        log.Append("second message");
        log.Append("third message");

        log.Text.ShouldBe($"second message{Environment.NewLine}third message");
    }
}
