using AStarDev.ScraperPlaying.WallpaperIngestion;
using AStarDev.Utilities;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>The main window. Renders <see cref="MainWindowViewModel"/> and the live preview, and forwards user actions to them.</summary>
public partial class MainWindow : Window
{
    private readonly MainWindowViewModel viewModel;
    private readonly IScrapeConfigurationFileService scrapeConfigurationFileService;
    private readonly ImageDisplayCoordinator imageDisplayCoordinator;
    private bool isImageDisplayEnabled = true;

    public MainWindow(MainWindowViewModel viewModel, IScrapeConfigurationFileService scrapeConfigurationFileService, ImageDisplayCoordinator imageDisplayCoordinator)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        this.scrapeConfigurationFileService = scrapeConfigurationFileService;
        this.imageDisplayCoordinator = imageDisplayCoordinator;
        viewModel.Changed += OnViewModelChanged;
        imageDisplayCoordinator.ImageReady += OnImageReady;
        Closed += OnClosed;
        Loaded += async (_, _) => await viewModel.CheckRootDirectoryAvailabilityAsync();
        RenderViewModel();
    }

    public async void ImportConfiguration(object? sender, RoutedEventArgs eventArgs)
        => await viewModel.ImportConfigurationAsync(cancellationToken => scrapeConfigurationFileService.ImportViaPickerAsync(this, cancellationToken));

    public async void ExportConfiguration(object? sender, RoutedEventArgs eventArgs)
        => await viewModel.ExportConfigurationAsync(cancellationToken => scrapeConfigurationFileService.ExportViaPickerAsync(this, cancellationToken));

    public async void RunScraper(object? sender, RoutedEventArgs eventArgs) => await viewModel.RunScraperAsync();

    public void CancelOperation(object? sender, RoutedEventArgs eventArgs) => viewModel.CancelOperation();

    public void ToggleImageDisplay(object? sender, RoutedEventArgs eventArgs)
    {
        isImageDisplayEnabled = ImageDisplayToggle.IsChecked == true;
        imageDisplayCoordinator.IsEnabled = isImageDisplayEnabled;
        if (!isImageDisplayEnabled) ClearDisplayedImage();
    }

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

    private void OnClosed(object? sender, EventArgs eventArgs)
    {
        viewModel.Changed -= OnViewModelChanged;
        imageDisplayCoordinator.ImageReady -= OnImageReady;
        viewModel.Dispose();
    }

    private void OnViewModelChanged(object? sender, EventArgs eventArgs) => Dispatcher.UIThread.Post(RenderViewModel);

    private void OnImageReady(object? sender, WallpaperPreviewImage preview) => Dispatcher.UIThread.Post(() => DisplayImage(preview));

    private void RenderViewModel()
    {
        if (StatusTextBlock.Text != viewModel.StatusText)
        {
            StatusTextBlock.Text = viewModel.StatusText;
            StatusScrollViewer.ScrollToEnd();
        }

        ImportConfigurationMenuItem.IsEnabled = viewModel.AreConfigurationOperationsEnabled;
        ExportConfigurationMenuItem.IsEnabled = viewModel.AreConfigurationOperationsEnabled;
        RunScraperButton.IsEnabled = viewModel.IsRunEnabled;
        CancelButton.IsEnabled = viewModel.IsCancelEnabled;
    }

    private void DisplayImage(WallpaperPreviewImage preview)
    {
        if (!isImageDisplayEnabled)
        {
            preview.PngStream.Dispose();

            return;
        }

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

    private void ClearDisplayedImage()
    {
        (DownloadedImage.Source as IDisposable)?.Dispose();
        DownloadedImage.Source = null;
        ImageDetailsPanel.IsVisible = false;
        ImageNameText.Text = string.Empty;
        ImageCategoryText.Text = string.Empty;
        ImageSizeText.Text = string.Empty;
        ImageDimensionsText.Text = string.Empty;
    }
}
