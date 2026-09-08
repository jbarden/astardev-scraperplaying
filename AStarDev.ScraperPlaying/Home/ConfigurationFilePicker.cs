using AStarDev.FunctionalParadigm;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace AStarDev.ScraperPlaying.Home;

public sealed class ConfigurationFilePicker : IConfigurationFilePicker
{
    public async Task<Option<string>> PickAsync(Window owner)
    {
        var options = new FilePickerOpenOptions
        {
            AllowMultiple = false,
            Title = "Import scrape configuration",
            FileTypeFilter = [new FilePickerFileType("JSON configuration") { Patterns = ["*.json"] }]
        };
        var files = await owner.StorageProvider.OpenFilePickerAsync(options);

        if (files.Count == 0)
        {
            return Option<string>.None.Instance;
        }

        return new Option<string>.Some(files[0].Path.LocalPath);
    }
}