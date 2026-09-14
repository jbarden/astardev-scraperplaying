namespace AStarDev.ScraperPlaying.Home;

/// <summary>Accumulates status messages up to a fixed capacity, discarding the oldest message once that capacity is exceeded, and renders the retained messages as newline-separated text.</summary>
/// <param name="maximumMessages">The maximum number of messages to retain.</param>
public sealed class StatusMessageLog(int maximumMessages)
{
    private readonly Queue<string> messages = new();

    /// <summary>The retained messages, newline-separated, oldest first.</summary>
    public string Text { get; private set; } = string.Empty;

    /// <summary>Appends <paramref name="message"/>, discarding the oldest message if over capacity, and refreshes <see cref="Text"/>.</summary>
    /// <param name="message">The message to append.</param>
    public void Append(string message)
    {
        messages.Enqueue(message);
        while (messages.Count > maximumMessages)
        {
            messages.Dequeue();
        }

        Text = string.Join(Environment.NewLine, messages);
    }
}
