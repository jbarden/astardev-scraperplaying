using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>
/// The application's main window. It only translates between controls and the collaborators that hold the logic: the status
/// reporter, readiness, operation runner, configuration browser and scrape runner.
/// </summary>
public partial class MainWindow : Window
{
    private readonly IScrapeConfigurationFileService scrapeConfigurationFileService;
    private readonly StatusReporter status;
    private readonly ApplicationReadiness readiness;
    private readonly UserOperationRunner operations;
    private readonly ConfigurationBrowser configurationBrowser;
    private readonly ScrapeRunner scrapeRunner;

    public MainWindow(StatusReporter status, ApplicationReadiness readiness, UserOperationRunner operations, ConfigurationBrowser configurationBrowser, ScrapeRunner scrapeRunner, IScrapeConfigurationFileService scrapeConfigurationFileService, ImageDisplayCoordinator imageDisplayCoordinator)
    {
        InitializeComponent();
        this.status = status;
        this.readiness = readiness;
        this.operations = operations;
        this.configurationBrowser = configurationBrowser;
        this.scrapeRunner = scrapeRunner;
        this.scrapeConfigurationFileService = scrapeConfigurationFileService;
        status.RefreshRequired += (_, _) => Dispatcher.UIThread.Post(RefreshStatusText);
        readiness.Changed += (_, _) => Dispatcher.UIThread.Post(UpdateControls);
        ImagePreview.Attach(imageDisplayCoordinator);
        Loaded += async (_, _) => await InitialiseAsync();
        UpdateControls();
    }

    public async void ImportConfiguration(object? sender, RoutedEventArgs eventArgs) =>
        await operations.RunAsync("Scrape configuration import cancelled.", "Unable to import scrape configuration.", async cancellationToken =>
        {
            status.Append(ConfigurationTransferMessages.ForImport(await scrapeConfigurationFileService.ImportViaPickerAsync(this, cancellationToken)));
            await RefreshConfigurationPickerAsync();
        });

    public async void ExportConfiguration(object? sender, RoutedEventArgs eventArgs) =>
        await operations.RunAsync("Scrape configuration export cancelled.", "Unable to export scrape configuration.", async cancellationToken =>
            status.Append(ConfigurationTransferMessages.ForExport(await scrapeConfigurationFileService.ExportViaPickerAsync(this, cancellationToken))));

    public void ConfigurationSelected(object? sender, SelectionChangedEventArgs eventArgs) => UpdateControls();

    public async void EditConfiguration(object? sender, RoutedEventArgs eventArgs)
    {
        if (ConfigurationPicker.SelectedItem is not ScrapeConfigurationSummary summary) return;

        if (await configurationBrowser.FindAsync(summary) is not Option<ScrapeConfigurationEntity>.Some found) return;

        var saved = await configurationBrowser.CreateEditor(found.Value).ShowDialog<bool>(this);
        if (saved) await RefreshConfigurationPickerAsync();
    }

    public async void RunScraper(object? sender, RoutedEventArgs eventArgs) => await scrapeRunner.RunAsync();

    public void CancelOperation(object? sender, RoutedEventArgs eventArgs) => operations.Cancel();

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

    private async Task InitialiseAsync()
    {
        if (await readiness.InitialiseAsync()) await RefreshConfigurationPickerAsync();
    }

    private async Task RefreshConfigurationPickerAsync()
    {
        var selectedId = (ConfigurationPicker.SelectedItem as ScrapeConfigurationSummary)?.Id;
        var summaries = await configurationBrowser.ListAsync();
        ConfigurationPicker.ItemsSource = summaries;
        ConfigurationPicker.SelectedItem = summaries.FirstOrDefault(summary => summary.Id == selectedId) ?? (summaries.Count > 0 ? summaries[0] : null);
        UpdateControls();
    }

    private void UpdateControls()
    {
        ImportConfigurationMenuItem.IsEnabled = readiness.CanOperate;
        ExportConfigurationMenuItem.IsEnabled = readiness.CanOperate;
        ConfigurationPicker.IsEnabled = readiness.CanOperate;
        EditConfigurationButton.IsEnabled = readiness.CanOperate && ConfigurationPicker.SelectedItem is ScrapeConfigurationSummary;
        RunScraperButton.IsEnabled = readiness.CanRunScraper;
        CancelButton.IsEnabled = readiness.IsOperationRunning;
    }

    private void RefreshStatusText()
    {
        StatusTextBlock.Text = status.Text;
        StatusScrollViewer.ScrollToEnd();
    }
}
