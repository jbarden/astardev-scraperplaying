using System.IO.Abstractions;
using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>Hosts the section tabs used to edit a single root scrape configuration. Save writes every section and closes with <c>true</c>, Cancel closes with <c>false</c>.</summary>
public partial class ConfigurationEditorWindow : Window
{
    private readonly ConfigurationBrowser browser = null!;
    private readonly ConfigurationEditSaver saver = null!;
    private readonly IFileSystem fileSystem = null!;
    private ScrapeConfigurationId configurationId;

    /// <summary>Initializes the window for the design-time previewer.</summary>
    public ConfigurationEditorWindow() => InitializeComponent();

    /// <summary>Initializes a new instance of the <see cref="ConfigurationEditorWindow"/> class for the specified configuration.</summary>
    /// <param name="configuration">The scrape configuration to edit first.</param>
    /// <param name="summaries">Every scrape configuration the user can switch to.</param>
    /// <param name="browser">The browser used to load the configuration the user switches to.</param>
    /// <param name="saver">The service that validates and saves the edits.</param>
    /// <param name="fileSystem">The file system used to check that the scrape directories exist.</param>
    public ConfigurationEditorWindow(ScrapeConfigurationEntity configuration, IReadOnlyList<ScrapeConfigurationSummary> summaries, ConfigurationBrowser browser, ConfigurationEditSaver saver, IFileSystem fileSystem)
    {
        InitializeComponent();
        this.browser = browser;
        this.saver = saver;
        this.fileSystem = fileSystem;
        ConfigurationPicker.ItemsSource = summaries;
        ConfigurationPicker.SelectedItem = summaries.FirstOrDefault(summary => summary.Id == configuration.Id);
        Load(configuration);
    }

    public async void ConfigurationSelected(object? sender, SelectionChangedEventArgs eventArgs)
    {
        if (ConfigurationPicker.SelectedItem is not ScrapeConfigurationSummary summary || summary.Id == configurationId) return;

        ShowError(string.Empty);
        if (await browser.FindAsync(summary) is Option<ScrapeConfigurationEntity>.Some found) Load(found.Value);
    }

    private void Load(ScrapeConfigurationEntity configuration)
    {
        configurationId = configuration.Id;
        RootSettingsTabContent.Load(RootSettingsInput.From(configuration));
        UserTabContent.Load(UserSettingsInput.From(configuration));
        SearchTabContent.Load(SearchSettingsInput.From(configuration));
        SearchCategoriesTabContent.Load(SearchCategoriesInput.From(configuration));
        PersonCategoriesTabContent.Load(PersonCategoriesInput.From(configuration));
        DirectoriesTabContent.Load(DirectorySettingsInput.From(configuration), fileSystem);
    }

    public async void Save(object? sender, RoutedEventArgs eventArgs)
    {
        ShowError(string.Empty);
        SetSaving(true);
        var failure = await saver.SaveAsync(configurationId, ReadInputs(), CancellationToken.None);
        SetSaving(false);
        if (failure is Option<string>.Some some) ShowError(some.Value);
        else Close(true);
    }

    public void Cancel(object? sender, RoutedEventArgs eventArgs) => Close(false);

    private ConfigurationEditInputs ReadInputs() => new(
        RootSettingsTabContent.ReadInput(),
        UserTabContent.ReadInput(),
        DirectoriesTabContent.ReadInput(),
        SearchTabContent.ReadInput(),
        SearchCategoriesTabContent.ReadInput(),
        PersonCategoriesTabContent.ReadInput());

    private void SetSaving(bool isSaving)
    {
        SaveButton.IsEnabled = !isSaving;
        CancelButton.IsEnabled = !isSaving;
    }

    private void ShowError(string message) => ErrorText.Text = message;
}
