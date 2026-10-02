using AStarDev.ScraperPlaying.WallpaperIngestion;
using AStarDev.Utilities;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>The live preview of the wallpaper most recently downloaded, with its details and the switch that turns the preview on and off.</summary>
public sealed partial class ImagePreviewPanel : UserControl
{
    private ImageDisplayCoordinator? coordinator;

    public ImagePreviewPanel() => InitializeComponent();

    /// <summary>Starts showing the images <paramref name="imageDisplayCoordinator"/> publishes, and lets the switch turn its decoding on and off.</summary>
    /// <param name="imageDisplayCoordinator">The source of the decoded preview images.</param>
    public void Attach(ImageDisplayCoordinator imageDisplayCoordinator)
    {
        coordinator = imageDisplayCoordinator;
        imageDisplayCoordinator.ImageReady += (_, preview) => Dispatcher.UIThread.Post(() => DisplayImage(preview));
    }

    public void ToggleImageDisplay(object? sender, RoutedEventArgs eventArgs)
    {
        if (coordinator is null) return;

        coordinator.IsEnabled = ImageDisplayToggle.IsChecked == true;
        if (!coordinator.IsEnabled) ClearDisplayedImage();
    }

    private void DisplayImage(WallpaperPreviewImage preview)
    {
        if (coordinator?.IsEnabled != true)
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

        ImageNameText.Text = preview.Info.Name;
        ImageCategoryText.Text = preview.Info.CategoryDescription;
        ImageSizeText.Text = preview.Info.FileSizeBytes.ToFileSizeString();
        ImageDimensionsText.Text = $"{preview.Info.Width} x {preview.Info.Height}";
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
