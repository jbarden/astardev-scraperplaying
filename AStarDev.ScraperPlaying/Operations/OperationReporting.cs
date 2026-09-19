using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.Operations;

/// <summary>How an operation reports what happened to it.</summary>
/// <param name="Report">Shows a message to the user.</param>
/// <param name="CancelledMessage">The message reported when the user cancels the operation.</param>
/// <param name="DescribeFailure">Gives the message to report for an expected failure, or <see cref="Option{T}.None"/> when the failure is unexpected and should propagate.</param>
public sealed record OperationReporting(Action<string> Report, string CancelledMessage, Func<Exception, Option<string>> DescribeFailure);
