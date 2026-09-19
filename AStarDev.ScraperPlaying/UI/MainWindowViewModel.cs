using System.Text.Json;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using AStarDev.ScraperPlaying.Scraping;
using Microsoft.Extensions.Logging;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>
/// The state and behaviour behind the main window: the status log, which controls are enabled, and running the scraper
/// and the import/export operations. The window only renders this state and forwards user actions, so none of it depends on Avalonia.
/// </summary>
public sealed class MainWindowViewModel : IDisposable
{
    private const int MaximumStatusMessages = 100;
    private readonly StatusMessageLog statusMessageLog = new(MaximumStatusMessages);
    private readonly IScrapeService scrapeService;
    private readonly OperationCoordinator operationCoordinator;
    private readonly ILogger<MainWindowViewModel> logger;
    private bool areRootDirectoriesAvailable = true;

    public MainWindowViewModel(IScrapeService scrapeService, OperationCoordinator operationCoordinator, ILogger<MainWindowViewModel> logger)
    {
        this.scrapeService = scrapeService;
        this.operationCoordinator = operationCoordinator;
        this.logger = logger;
        operationCoordinator.StateChanged += OnOperationStateChanged;
    }

    /// <summary>Raised, possibly from a background thread, whenever any of the view model's state changes.</summary>
    public event EventHandler? Changed;

    /// <summary>The text shown in the status area.</summary>
    public string StatusText { get; private set; } = "Ready for action.";

    /// <summary>Whether the configuration import/export menu items are enabled.</summary>
    public bool AreConfigurationOperationsEnabled => !operationCoordinator.IsOperationRunning;

    /// <summary>Whether the run scraper button is enabled.</summary>
    public bool IsRunEnabled => !operationCoordinator.IsOperationRunning && areRootDirectoriesAvailable;

    /// <summary>Whether the cancel button is enabled.</summary>
    public bool IsCancelEnabled => operationCoordinator.IsOperationRunning;

    /// <summary>Appends a message to the status log and shows the log.</summary>
    /// <param name="message">The message to append.</param>
    public void AppendStatusMessage(string message)
    {
        statusMessageLog.Append(message);
        StatusText = statusMessageLog.Text;
        OnChanged();
    }

    /// <summary>Runs the scraper, showing its progress in the status log.</summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task RunScraperAsync() => scrapeService.RunScraperAsync(new Progress<string>(AppendStatusMessage));

    /// <summary>Requests cancellation of the running operation.</summary>
    public void CancelOperation() => operationCoordinator.Cancel();

    /// <summary>Checks the root directories are usable, reporting each problem, and updates whether the scraper can run.</summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CheckRootDirectoryAvailabilityAsync()
    {
        var problems = await scrapeService.ValidateRootDirectoriesAsync();
        areRootDirectoriesAvailable = problems.Count == 0;
        foreach (var problem in problems) AppendStatusMessage(problem);

        OnChanged();
    }

    /// <summary>Runs a configuration import as the current operation and reports its outcome.</summary>
    /// <param name="import">Performs the import (for example by asking the user for a file) and reports whether it completed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task ImportConfigurationAsync(Func<CancellationToken, Task<Option<Unit>>> import)
        => RunConfigurationOperationAsync(
            async cancellationToken => (await import(cancellationToken)).Match(
                _ => "Scrape configuration imported.",
                () => "Scrape configuration import could not be completed."),
            "import",
            exception => exception is IOException or JsonException or InvalidOperationException);

    /// <summary>Runs a configuration export as the current operation and reports its outcome.</summary>
    /// <param name="export">Performs the export (for example by asking the user for a file) and reports whether a configuration was written.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task ExportConfigurationAsync(Func<CancellationToken, Task<Option<bool>>> export)
        => RunConfigurationOperationAsync(
            async cancellationToken => (await export(cancellationToken)).Match(
                exported => exported ? "Scrape configuration exported." : "No scrape configuration was found to export.",
                () => "Scrape configuration export could not be completed."),
            "export",
            exception => exception is IOException or InvalidOperationException);

    /// <inheritdoc/>
    public void Dispose() => operationCoordinator.StateChanged -= OnOperationStateChanged;

    private async Task RunConfigurationOperationAsync(Func<CancellationToken, Task<string>> operation, string operationName, Func<Exception, bool> isExpectedFailure)
    {
        if (!operationCoordinator.TryStart(out var cancellationToken)) return;

        try
        {
            StatusText = await operation(cancellationToken);
            OnChanged();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            AppendStatusMessage($"Scrape configuration {operationName} cancelled.");
        }
        catch (Exception exception) when (isExpectedFailure(exception))
        {
            var message = $"Unable to {operationName} scrape configuration.";
            LogMessage.Error(logger, message, exception);
            AppendStatusMessage($"{message} {exception.Message}");
        }
        finally
        {
            operationCoordinator.Complete();
        }
    }

    private void OnOperationStateChanged(object? sender, EventArgs eventArgs) => OnChanged();

    private void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
