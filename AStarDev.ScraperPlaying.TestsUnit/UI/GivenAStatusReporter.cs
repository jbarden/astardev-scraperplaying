using AStarDev.ScraperPlaying.UI;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.ScraperPlaying.TestsUnit.UI;

public sealed class GivenAStatusReporter
{
    private readonly StatusReporter reporter = new(NullLogger<StatusReporter>.Instance);

    [Fact]
    public void when_messages_are_appended_then_the_text_lists_them_oldest_first()
    {
        reporter.Append("first");
        reporter.Append("second");

        reporter.Text.ShouldBe($"first{Environment.NewLine}second");
    }

    [Fact]
    public void when_the_text_has_not_been_read_then_a_burst_of_messages_raises_refresh_required_once()
    {
        var raised = 0;
        reporter.RefreshRequired += (_, _) => raised++;

        reporter.Append("first");
        reporter.Append("second");
        reporter.Append("third");

        raised.ShouldBe(1);
    }

    [Fact]
    public void when_the_text_is_read_then_the_next_message_raises_refresh_required_again()
    {
        var raised = 0;
        reporter.RefreshRequired += (_, _) => raised++;
        reporter.Append("first");
        _ = reporter.Text;

        reporter.Append("second");

        raised.ShouldBe(2);
    }

    [Fact]
    public void when_an_error_is_reported_then_the_message_and_the_exception_message_are_appended()
    {
        reporter.Error("Unable to do the thing.", new InvalidOperationException("it broke"));

        reporter.Text.ShouldBe("Unable to do the thing. it broke");
    }

    [Fact]
    public void when_an_error_has_an_inner_exception_then_its_message_is_appended_too()
    {
        reporter.Error("Unable to save.", new InvalidOperationException("An error occurred while saving the entity changes.", new IOException("UNIQUE constraint failed: FileDetail.FileHandle")));

        reporter.Text.ShouldBe("Unable to save. An error occurred while saving the entity changes. Caused by: UNIQUE constraint failed: FileDetail.FileHandle");
    }
}
