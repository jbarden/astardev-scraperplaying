using AStarDev.FunctionalParadigm;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace AStarDev.ScraperPlaying.Home;

public sealed class ConfigurationFilePicker : IConfigurationFilePicker
{
    public async Task<Option<string>> PickAsync(Window owner)
    {
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            Title = "Import scrape configuration",
            FileTypeFilter = [new FilePickerFileType("JSON configuration") { Patterns = ["*.json"] }]
        });

        return files.Count == 0 ? null! : files[0].Path.LocalPath;
    }

    public async Task<Option<string>> PickSaveAsync(Window owner)
    {
        var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export scrape configuration",
            SuggestedFileName = "scrape-configuration.json",
            DefaultExtension = "json",
            FileTypeChoices = [new FilePickerFileType("JSON configuration") { Patterns = ["*.json"] }]
        });

        return file is null ? null! : file.Path.LocalPath;
    }
}