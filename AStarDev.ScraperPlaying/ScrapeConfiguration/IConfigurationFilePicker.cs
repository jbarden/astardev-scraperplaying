using AStarDev.FunctionalParadigm;
using Avalonia.Controls;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

public interface IConfigurationFilePicker
{
    Task<Option<string>> PickAsync(Window owner);

    Task<Option<string>> PickSaveAsync(Window owner);
}
