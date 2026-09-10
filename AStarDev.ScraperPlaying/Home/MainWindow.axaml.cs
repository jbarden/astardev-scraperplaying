using System.Text.Json;
using AStar.Dev.Logging.Extensions;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.SearchAPI;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Interactivity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AStarDev.ScraperPlaying.Home;

public partial class MainWindow : Window, IDisposable
{
    private const int MaximumStatusMessages = 100;
    private readonly Queue<string> statusMessages = new();
    private readonly IScrapeConfigurationImportService importService;
    private readonly IConfigurationFilePicker configurationFilePicker;
    private readonly IScrapeService scrapeService;
    private readonly ILogger<MainWindow> logger;
    private readonly OperationCoordinator operationCoordinator;
    private bool isDisposing;

    public MainWindow(ILogger<MainWindow> logger, IScrapeConfigurationImportService importService, IConfigurationFilePicker configurationFilePicker, IScrapeService scrapeService, OperationCoordinator operationCoordinator)
    {
        InitializeComponent();
        this.importService = importService;
        this.configurationFilePicker = configurationFilePicker;
        this.scrapeService = scrapeService;
        this.logger = logger;
        this.operationCoordinator = operationCoordinator;
        operationCoordinator.StateChanged += (_, _) => UpdateOperationControls();
        Closed += (_, _) => Dispose();
        UpdateOperationControls();
    }

    public static MainWindow CreateStartupError(Exception exception)
    {
        // operationCoordinator must be non-null: the constructor subscribes to its StateChanged event
        var window = new MainWindow(NullLogger<MainWindow>.Instance, null!, null!, null!, new OperationCoordinator());
        window.AppendStatusMessage($"Startup failed: {exception.GetType().Name}: {exception.Message}");
        return window;
    }

    public async void ImportConfiguration(object? sender, RoutedEventArgs eventArgs)
    {
        if (!operationCoordinator.TryStart(out var cancellationToken)) return;

        try
        {
            await configurationFilePicker.PickAsync(this).MatchAsync(
                async path =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await importService.ImportAsync(path, cancellationToken);
                    StatusTextBlock.Text = "Scrape configuration imported.";
                },
                () => StatusTextBlock.Text = "Scrape configuration import could not be completed."
            );
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            AppendStatusMessage("Scrape configuration import cancelled.");
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidOperationException)
        {
            LogError("Unable to import scrape configuration.", exception);
        }
        finally
        {
            operationCoordinator.Complete();
        }
    }

    public async void RunScraper(object? sender, RoutedEventArgs eventArgs)
    {
        var progress = new Progress<string>(AppendStatusMessage);
        await scrapeService.RunScraperAsync(progress);
    }

    public void CancelOperation(object? sender, RoutedEventArgs eventArgs) => operationCoordinator.Cancel();

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
            // NAR at the current time
        }

        isDisposing = true;
    }

    private void LogError(string message, Exception exception)
    {
        LogMessage.Error(logger, message, exception);
        AppendStatusMessage($"{message} {exception.Message}");
    }

    private void UpdateOperationControls()
    {
        var isOperationRunning = operationCoordinator.IsOperationRunning;
        ImportConfigurationButton.IsEnabled = !isOperationRunning;
        RunScraperButton.IsEnabled = !isOperationRunning;
        CancelButton.IsEnabled = isOperationRunning;
    }

    private void AppendStatusMessage(string message) => Dispatcher.UIThread.Post(() =>
                                                             {
                                                                 statusMessages.Enqueue(message);
                                                                 while (statusMessages.Count > MaximumStatusMessages)
                                                                 {
                                                                     statusMessages.Dequeue();
                                                                 }

                                                                 StatusTextBlock.Text = string.Join(Environment.NewLine, statusMessages);
                                                                 StatusScrollViewer.ScrollToEnd();
                                                             });
}

