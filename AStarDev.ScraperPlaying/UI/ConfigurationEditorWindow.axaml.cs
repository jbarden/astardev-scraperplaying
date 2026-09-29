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
    private readonly IScrapeConfigurationUpdater updater = null!;

    /// <summary>Initializes the window for the design-time previewer.</summary>
    public ConfigurationEditorWindow() => InitializeComponent();

    /// <summary>Initializes a new instance of the <see cref="ConfigurationEditorWindow"/> class for the specified configuration.</summary>
    /// <param name="configuration">The scrape configuration to edit.</param>
    /// <param name="updater">The service used to save the edits.</param>
    /// <param name="fileSystem">The file system used to check that the scrape directories exist.</param>
    public ConfigurationEditorWindow(ScrapeConfigurationEntity configuration, IScrapeConfigurationUpdater updater, IFileSystem fileSystem)
    {
        InitializeComponent();
        configurationId = configuration.Id;
        this.updater = updater;
        ConfigurationLabelText.Text = ScrapeConfigurationSummary.From(configuration).Label;
        RootSettingsTabContent.Load(RootSettingsInput.From(configuration));
        UserTabContent.Load(UserSettingsInput.From(configuration));
        DirectoriesTabContent.Load(DirectorySettingsInput.From(configuration), fileSystem);
    }

    public async void Save(object? sender, RoutedEventArgs eventArgs)
    {
        var rootSettings = RootSettingsTabContent.ReadInput().Validate();
        var userSettings = UserTabContent.ReadInput().Validate();
        var directorySettings = DirectoriesTabContent.ReadInput().Validate();
        var errors = CollectErrors(rootSettings).Concat(CollectErrors(userSettings)).Concat(CollectErrors(directorySettings)).ToList();
        if (errors.Count > 0)
        {
            ShowError(string.Join(Environment.NewLine, errors.Select(error => $"{error.Property}: {error.Message}")));

            return;
        }

        ShowError(string.Empty);
        SetSaving(true);
        var result = await updater.SaveAsync(configurationId, [((Valid<RootSettings>)rootSettings).Value, ((Valid<UserSettings>)userSettings).Value, ((Valid<DirectorySettings>)directorySettings).Value], CancellationToken.None);
        var failure = result.Match(
            saved => saved.Match(_ => Option.None<string>(), () => Option.Some("The scrape configuration no longer exists.")),
            exception => Option.Some($"Unable to save the scrape configuration. {exception.Message}"));
        SetSaving(false);
        if (failure is Option<string>.Some some) ShowError(some.Value);
        else Close(true);
    }

    public void Cancel(object? sender, RoutedEventArgs eventArgs) => Close(false);

    private static IReadOnlyList<ValidationError> CollectErrors<T>(Validation<T> validation) =>
        validation is Invalid<T> invalid ? invalid.Errors : [];

    private void SetSaving(bool isSaving)
    {
        SaveButton.IsEnabled = !isSaving;
        CancelButton.IsEnabled = !isSaving;
    }

    private void ShowError(string message) => ErrorText.Text = message;
}
