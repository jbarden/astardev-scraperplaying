using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>Hosts the section tabs used to edit a single root scrape configuration. Save closes with <c>true</c>, Cancel with <c>false</c>.</summary>
public partial class ConfigurationEditorWindow : Window
{
    /// <summary>Initializes the window for the design-time previewer.</summary>
    public ConfigurationEditorWindow() => InitializeComponent();

    /// <summary>Initializes a new instance of the <see cref="ConfigurationEditorWindow"/> class for the specified configuration.</summary>
    /// <param name="configuration">The scrape configuration to edit.</param>
    public ConfigurationEditorWindow(ScrapeConfigurationEntity configuration)
    {
        InitializeComponent();
        ConfigurationLabelText.Text = ScrapeConfigurationSummary.From(configuration).Label;
    }

    public void Save(object? sender, RoutedEventArgs eventArgs) => Close(true);

    public void Cancel(object? sender, RoutedEventArgs eventArgs) => Close(false);
}
