using Avalonia.Controls;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>Shown instead of the main window when the application fails to start.</summary>
public partial class StartupErrorWindow : Window
{
    public StartupErrorWindow(Exception exception)
    {
        InitializeComponent();
        ErrorTextBlock.Text = $"Startup failed: {exception.GetType().Name}: {exception.Message}";
    }
}
