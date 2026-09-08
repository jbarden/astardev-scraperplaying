using AStarDev.FunctionalParadigm;
using Avalonia.Controls;

namespace AStarDev.ScraperPlaying.Home;

public interface IConfigurationFilePicker
{
    Task<Option<string>> PickAsync(Window owner);
}
