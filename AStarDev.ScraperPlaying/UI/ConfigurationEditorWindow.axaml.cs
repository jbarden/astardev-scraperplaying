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
    private readonly ScrapeConfigurationId configurationId;
    private readonly ConfigurationEditSaver saver = null!;

    /// <summary>Initializes the window for the design-time previewer.</summary>
    public ConfigurationEditorWindow() => InitializeComponent();

    /// <summary>Initializes a new instance of the <see cref="ConfigurationEditorWindow"/> class for the specified configuration.</summary>
    /// <param name="configuration">The scrape configuration to edit.</param>
    /// <param name="saver">The service that validates and saves the edits.</param>
    /// <param name="fileSystem">The file system used to check that the scrape directories exist.</param>
    public ConfigurationEditorWindow(ScrapeConfigurationEntity configuration, ConfigurationEditSaver saver, IFileSystem fileSystem)
    {
        InitializeComponent();
        configurationId = configuration.Id;
        this.saver = saver;
        ConfigurationLabelText.Text = ScrapeConfigurationSummary.From(configuration).Label;
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
