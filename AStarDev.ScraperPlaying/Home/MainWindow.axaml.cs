using System.Text.Json;
using AStar.Dev.Logging.Extensions;
using AStarDev.FunctionalParadigm;
using AStarDev.Utilities;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.Input;
using Avalonia.Interactivity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Testably.Abstractions;

namespace AStarDev.ScraperPlaying.Home;

public partial class MainWindow : Window, IDisposable
{
    private const int MaximumStatusMessages = 100;
    private readonly StatusMessageLog statusMessageLog = new(MaximumStatusMessages);
    private readonly IScrapeConfigurationFileService scrapeConfigurationFileService;
    private readonly IScrapeService scrapeService;
    private readonly ILogger<MainWindow> logger;
    private readonly OperationCoordinator operationCoordinator;
    private bool isDisposing;
    private bool isRootDirectoryAvailable = true;

    public MainWindow(ILogger<MainWindow> logger, IScrapeConfigurationFileService scrapeConfigurationFileService, IScrapeService scrapeService, OperationCoordinator operationCoordinator, ImageDisplayCoordinator imageDisplayCoordinator)
    {
        InitializeComponent();
        this.scrapeConfigurationFileService = scrapeConfigurationFileService;
        this.scrapeService = scrapeService;
        this.logger = logger;
        this.operationCoordinator = operationCoordinator;
        operationCoordinator.StateChanged += (_, _) => UpdateOperationControls();
        imageDisplayCoordinator.ImageReady += (_, preview) => Dispatcher.UIThread.Post(() => DisplayImage(preview));
        Closed += (_, _) => Dispose();
        Loaded += async (_, _) => await CheckRootDirectoryAvailabilityAsync();
        UpdateOperationControls();
    }

    public static MainWindow CreateStartupError(Exception exception)
    {
        // operationCoordinator/imageDisplayCoordinator must be non-null: the constructor subscribes to their events
        var window = new MainWindow(NullLogger<MainWindow>.Instance, null!, null!, new OperationCoordinator(), new ImageDisplayCoordinator(new ImageDownloadNotifier(), new DownloadedImageDecoder(new RealFileSystem())));
        window.AppendStatusMessage($"Startup failed: {exception.GetType().Name}: {exception.Message}");

        return window;
    }

    public async void ImportConfiguration(object? sender, RoutedEventArgs eventArgs)
    {
        if (!operationCoordinator.TryStart(out var cancellationToken)) return;

        try
        {
            var message = (await scrapeConfigurationFileService.ImportViaPickerAsync(this, cancellationToken)).Match(
                _ => "Scrape configuration imported.",
                () => "Scrape configuration import could not be completed.");
            SetStatusText(message);
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

    public async void ExportConfiguration(object? sender, RoutedEventArgs eventArgs)
    {
        if (!operationCoordinator.TryStart(out var cancellationToken)) return;

        try
        {
            var message = (await scrapeConfigurationFileService.ExportViaPickerAsync(this, cancellationToken)).Match(
                exported => exported ? "Scrape configuration exported." : "No scrape configuration was found to export.",
                () => "Scrape configuration export could not be completed.");
            SetStatusText(message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            AppendStatusMessage("Scrape configuration export cancelled.");
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException)
        {
            LogError("Unable to export scrape configuration.", exception);
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

    public void Exit(object? sender, RoutedEventArgs eventArgs) => Close();

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.F4 && e.KeyModifiers == KeyModifiers.Alt)
        {
            Close();

            return;
        }

        base.OnKeyDown(e);
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
        ImportConfigurationMenuItem.IsEnabled = !isOperationRunning;
        ExportConfigurationMenuItem.IsEnabled = !isOperationRunning;
        RunScraperButton.IsEnabled = !isOperationRunning && isRootDirectoryAvailable;
        CancelButton.IsEnabled = isOperationRunning;
    }

    private async Task CheckRootDirectoryAvailabilityAsync()
    {
        if (scrapeService is null) return;

        isRootDirectoryAvailable = await scrapeService.RootDirectoryExistsAsync();
        if (!isRootDirectoryAvailable)
        {
            AppendStatusMessage("Root directory could not be found.");
        }

        UpdateOperationControls();
    }

    private void DisplayImage(WallpaperPreviewImage preview)
    {
        var previousImage = DownloadedImage.Source;
        using (preview.PngStream)
        {
            DownloadedImage.Source = new Bitmap(preview.PngStream);
        }

        (previousImage as IDisposable)?.Dispose();

        ImageNameText.Text = preview.Name;
        ImageCategoryText.Text = $"Category: {preview.CategoryLabel}";
        ImageSizeText.Text = $"Size: {preview.FileSizeBytes.ToFileSizeString()}";
        ImageDimensionsText.Text = $"Dimensions: {preview.Width} x {preview.Height}";
        ImageDetailsPanel.IsVisible = true;
    }

    private void SetStatusText(string message) => Dispatcher.UIThread.Post(() => StatusTextBlock.Text = message);

    private void AppendStatusMessage(string message) => Dispatcher.UIThread.Post(() =>
                                                             {
                                                                 statusMessageLog.Append(message);
                                                                 StatusTextBlock.Text = statusMessageLog.Text;
                                                                 StatusScrollViewer.ScrollToEnd();
                                                             });
}

