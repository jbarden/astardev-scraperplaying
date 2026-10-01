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
            status.Append(ConfigurationTransferMessages.ForImport(await scrapeConfigurationFileService.ImportViaPickerAsync(this, cancellationToken))));

    public void ExportConfiguration(object? sender, RoutedEventArgs eventArgs) => Export(ApiKeyExport.Exclude);

    public void ExportConfigurationWithApiKeys(object? sender, RoutedEventArgs eventArgs) => Export(ApiKeyExport.Include);

    public async void EditConfiguration(object? sender, RoutedEventArgs eventArgs) =>
        await operations.ReportFailuresAsync("Unable to edit scrape configuration.", async () =>
        {
            if (await configurationBrowser.CreateEditorAsync() is Option<ConfigurationEditorWindow>.Some editor) _ = await editor.Value.ShowDialog<bool>(this);
        });

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

    private async void Export(ApiKeyExport apiKeys) =>
        await operations.RunAsync("Scrape configuration export cancelled.", "Unable to export scrape configuration.", async cancellationToken =>
            status.Append(ConfigurationTransferMessages.ForExport(await scrapeConfigurationFileService.ExportViaPickerAsync(this, apiKeys, cancellationToken), apiKeys)));

    private async Task InitialiseAsync() => await readiness.InitialiseAsync();

    private void UpdateControls()
    {
        ImportConfigurationMenuItem.IsEnabled = readiness.CanOperate;
        ExportConfigurationMenuItem.IsEnabled = readiness.CanOperate;
        ExportConfigurationWithApiKeysMenuItem.IsEnabled = readiness.CanOperate;
        EditConfigurationMenuItem.IsEnabled = readiness.CanOperate;
        RunScraperButton.IsEnabled = readiness.CanRunScraper;
        CancelButton.IsEnabled = readiness.IsOperationRunning;
    }

    private void RefreshStatusText()
    {
        StatusTextBlock.Text = status.Text;
        StatusScrollViewer.ScrollToEnd();
    }
}
