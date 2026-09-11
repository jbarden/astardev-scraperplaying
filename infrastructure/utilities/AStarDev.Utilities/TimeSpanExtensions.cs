namespace AStarDev.Utilities;

/// <summary>
/// Provides extension methods for the <see cref="TimeSpan"/> struct.
/// </summary>
public static class TimeSpanExtensions
{
    /// <summary>
    /// Determines whether the specified <see cref="TimeSpan"/> represents a time span of less than two minutes.
    /// </summary>
    /// <param name="elapsed">The time span to evaluate.</param>
    /// <returns><c>true</c> if the time span is less than two minutes; otherwise, <c>false</c>.</returns>
    public static bool IsJustNow(this TimeSpan elapsed) => elapsed.TotalMinutes < 2;

    /// <summary>
    /// Determines whether the specified <see cref="TimeSpan"/> represents a time span of less than one hour.
    /// </summary>
    /// <param name="elapsed">The time span to evaluate.</param>
    /// <returns><c>true</c> if the time span is less than one hour; otherwise, <c>false</c>.</returns>
    public static bool IsMinutesAgo(this TimeSpan elapsed) => elapsed.TotalHours < 1;

    /// <summary>
    /// Determines whether the specified <see cref="TimeSpan"/> represents a time span of less than one day.
    /// </summary>
    /// <param name="elapsed">The time span to evaluate.</param>
    /// <returns><c>true</c> if the time span is less than one day; otherwise, <c>false</c>.</returns>
    public static bool IsHoursAgo(this TimeSpan elapsed) => elapsed.TotalDays < 1;

    /// <summary>
    /// Determines whether the specified <see cref="TimeSpan"/> represents a time span of less than two days.
    /// </summary>
    /// <param name="elapsed">The time span to evaluate.</param>
    /// <returns><c>true</c> if the time span is less than two days; otherwise, <c>false</c>.</returns>
    public static bool IsYesterday(this TimeSpan elapsed) => elapsed.TotalDays < 2;

    /// <summary>
    /// Formats the specified <see cref="TimeSpan"/> using whichever unit reads best for its magnitude:
    /// seconds when under a minute, minutes when under an hour, otherwise hours.
    /// </summary>
    /// <param name="elapsed">The time span to format.</param>
    /// <returns>A human-readable duration, e.g. "42 seconds", "3 minutes", or "2.5 hours".</returns>
    public static string ToDurationString(this TimeSpan elapsed)
    => elapsed.TotalSeconds < 60
        ? $"{elapsed.TotalSeconds:0.##} seconds"
        : elapsed.TotalMinutes < 60
            ? $"{elapsed.TotalMinutes:0.##} minutes"
            : $"{elapsed.TotalHours:0.##} hours";
}
