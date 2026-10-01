using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Tags;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AStarDev.ScraperPlaying.UI;

/// <summary>Lists the stored tags so the flags of each can be changed. Save writes the changed flags and closes with <c>true</c>, Cancel closes with <c>false</c>.</summary>
public sealed partial class TagsEditorWindow : Window
{
    private readonly IReadOnlyList<TagRow> rows = [];
    private readonly ITagCatalogue catalogue = null!;

    /// <summary>Initializes the window for the design-time previewer.</summary>
    public TagsEditorWindow() => InitializeComponent();

    /// <summary>Initializes a new instance of the <see cref="TagsEditorWindow"/> class over the specified tags.</summary>
    /// <param name="rows">The tags to edit.</param>
    /// <param name="catalogue">The service the changed flags are saved through.</param>
    public TagsEditorWindow(IReadOnlyList<TagRow> rows, ITagCatalogue catalogue)
    {
        InitializeComponent();
        this.rows = rows;
        this.catalogue = catalogue;
        TagsList.ItemsSource = rows;
    }

    public void FilterChanged(object? sender, TextChangedEventArgs eventArgs) =>
        TagsList.ItemsSource = rows.Where(row => row.Matches(FilterTextBox.Text ?? string.Empty)).ToList();

    public async void Save(object? sender, RoutedEventArgs eventArgs)
    {
        ErrorText.Text = string.Empty;
        SetSaving(true);
        var changes = rows.Where(row => row.IsChanged).ToDictionary(row => row.WallhavenTagId, row => row.Flags);
        var result = await catalogue.SaveFlagsAsync(changes);
        SetSaving(false);
        var failure = result.Match(_ => Option.None<string>(), exception => Option.Some($"Unable to save the tags. {exception.Message}"));
        if (failure is Option<string>.Some some) ErrorText.Text = some.Value;
        else Close(true);
    }

    public void Cancel(object? sender, RoutedEventArgs eventArgs) => Close(false);

    private void SetSaving(bool isSaving)
    {
        SaveButton.IsEnabled = !isSaving;
        CancelButton.IsEnabled = !isSaving;
    }
}
