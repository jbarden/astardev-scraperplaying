using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.ScraperPlaying.Operations;

/// <summary>Runs work as the application's single current operation: starts it, reports how it ended, and always completes it.</summary>
/// <param name="coordinator">The coordinator that allows only one operation at a time and supplies its cancellation token.</param>
/// <param name="logger">The logger used to record expected failures.</param>
public sealed class OperationRunner(OperationCoordinator coordinator, ILogger<OperationRunner> logger)
{
    /// <summary>Runs <paramref name="work"/> unless another operation is already running.</summary>
    /// <param name="work">The work to run, given the operation's cancellation token.</param>
    /// <param name="reporting">How to report cancellation and expected failures. Unexpected failures propagate.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RunAsync(Func<CancellationToken, Task> work, OperationReporting reporting)
    {
        if (!coordinator.TryStart(out var cancellationToken)) return;

        try
        {
            await work(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            reporting.Report(reporting.CancelledMessage);
        }
        catch (Exception exception)
        {
            if (reporting.DescribeFailure(exception) is not Option<string>.Some described) throw;

            LogMessage.Error(logger, described.Value, exception);
            reporting.Report(described.Value);
        }
        finally
        {
            coordinator.Complete();
        }
    }
}
