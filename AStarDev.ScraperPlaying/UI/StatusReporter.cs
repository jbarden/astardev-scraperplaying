using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>
/// Collects the user-facing status messages shown in the main window and logs the errors among them. It knows nothing about controls:
/// the window listens for <see cref="RefreshRequired"/> and renders <see cref="Text"/>. Safe to use from any thread.
/// </summary>
/// <param name="logger">The logger errors are written to.</param>
public sealed class StatusReporter(ILogger<StatusReporter> logger)
{
    private const int MaximumMessages = 100;
    private readonly StatusMessageLog log = new(MaximumMessages);

    /// <summary>Raised when the displayed text is out of date; once per burst of messages until <see cref="Text"/> is next read.</summary>
    public event EventHandler? RefreshRequired;

    /// <summary>The retained messages, newline-separated, oldest first.</summary>
    public string Text => log.Text;

    /// <summary>Appends a message to the status.</summary>
    /// <param name="message">The message to show the user.</param>
    public void Append(string message)
    {
        if (log.Append(message)) RefreshRequired?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Logs a failure and tells the user about it.</summary>
    /// <param name="message">What could not be done.</param>
    /// <param name="exception">Why it could not be done.</param>
    public void Error(string message, Exception exception)
    {
        LogMessage.Error(logger, message, exception);
        Append($"{message} {exception.Message}");
    }
}
