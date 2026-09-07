namespace AStarDev.ScraperPlaying.Home;

public sealed class OperationCoordinator : IDisposable
{
    private CancellationTokenSource? cancellationTokenSource;
    private bool isDisposing;

    public bool IsOperationRunning => cancellationTokenSource is not null;

    public event EventHandler? StateChanged;

    public bool TryStart(out CancellationToken cancellationToken)
    {
        if (cancellationTokenSource is not null)
        {
            cancellationToken = default;
            return false;
        }

        cancellationTokenSource = new CancellationTokenSource();
        cancellationToken = cancellationTokenSource.Token;
        StateChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public void Cancel() => cancellationTokenSource?.Cancel();

    public void Complete()
    {
        cancellationTokenSource?.Dispose();
        cancellationTokenSource = null;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (isDisposing) return;

        if (disposing)
        {
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
        }

        isDisposing = true;
    }
}