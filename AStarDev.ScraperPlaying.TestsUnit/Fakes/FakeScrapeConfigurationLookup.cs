using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.TestsUnit.Fakes;

/// <summary>A scrape configuration lookup whose results are set by the test.</summary>
internal sealed class FakeScrapeConfigurationLookup : IScrapeConfigurationLookup
{
    /// <summary>The result of <see cref="ListHeadersAsync"/>.</summary>
    public Exceptional<IReadOnlyList<ScrapeConfigurationHeader>> Headers { get; set; } = Exceptional.Success<IReadOnlyList<ScrapeConfigurationHeader>>([]);

    /// <summary>The result of <see cref="TryGetRootDirectoryAsync"/>.</summary>
    public Exceptional<Option<string>> RootDirectory { get; set; } = Exceptional.Success(Option.None<string>());

    public Task<Exceptional<IReadOnlyList<ScrapeConfigurationHeader>>> ListHeadersAsync(CancellationToken cancellationToken = default) => Task.FromResult(Headers);

    public Task<Exceptional<Option<string>>> TryGetRootDirectoryAsync(CancellationToken cancellationToken = default) => Task.FromResult(RootDirectory);
}
