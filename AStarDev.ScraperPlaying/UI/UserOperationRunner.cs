using AStarDev.ScraperPlaying.Operations;
using System.Text.Json;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>Runs a user-initiated operation as the single active operation, reporting cancellation and expected failures to the user instead of throwing.</summary>
/// <param name="operationCoordinator">Tracks whether an operation is running and lets it be cancelled.</param>
/// <param name="status">Where cancellation and failure messages are reported.</param>
public sealed class UserOperationRunner(OperationCoordinator operationCoordinator, StatusReporter status)
{
    /// <summary>Runs <paramref name="operation"/> unless another operation is already running.</summary>
    /// <param name="cancelledMessage">Reported when the operation is cancelled.</param>
    /// <param name="failureMessage">Reported, followed by the exception's message, when the operation fails with an I/O, JSON or invalid-operation error.</param>
    /// <param name="operation">The operation, given the token that <see cref="Cancel"/> cancels.</param>
    public async Task RunAsync(string cancelledMessage, string failureMessage, Func<CancellationToken, Task> operation)
    {
        if (!operationCoordinator.TryStart(out var cancellationToken)) return;

        try
        {
            await operation(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            status.Append(cancelledMessage);
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidOperationException)
        {
            status.Error(failureMessage, exception);
        }
        finally
        {
            operationCoordinator.Complete();
        }
    }

    /// <summary>Cancels the running operation, if any.</summary>
    public void Cancel() => operationCoordinator.Cancel();
}
