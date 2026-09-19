using System.IO.Abstractions;
using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.Scraping;

/// <inheritdoc/>
public class RootDirectoryValidator(IFileSystem fileSystem) : IRootDirectoryValidator
{
    /// <inheritdoc/>
    public IReadOnlyList<string> Validate(ScrapeDirectoriesEntity directories)
    {
        List<string> problems = [];
        if (!fileSystem.Directory.Exists(directories.RootDirectory)) problems.Add("Root directory could not be found.");

        if (string.IsNullOrWhiteSpace(directories.RootDirectoryFamous)) problems.Add("Famous root directory is not configured.");
        else if (!fileSystem.Directory.Exists(directories.RootDirectoryFamous)) problems.Add("Famous root directory could not be found.");

        return problems;
    }
}
