using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.TestsUnit.Fakes;

/// <summary>A files query whose existence result is set by the test and which records the names it was asked about.</summary>
internal sealed class FakeFilesQuery : IFilesQuery
{
    /// <summary>The result of <see cref="CheckExistsByNameAsync"/>; when <c>true</c>, <see cref="GetExistingNamesAsync"/> reports every supplied name as existing, and when a failure, it returns that failure.</summary>
    public Exceptional<bool> ExistsResult { get; set; } = false;

    /// <summary>The names <see cref="GetExistingNamesAsync"/> reports as existing when <see cref="ExistsResult"/> is <c>false</c>; matched case-insensitively, like the database collation.</summary>
    public List<FileName> ExistingNames { get; } = [];

    /// <summary>The number of times <see cref="GetExistingNamesAsync"/> was called.</summary>
    public int ExistingNamesQueryCount { get; private set; }

    /// <summary>The names passed to <see cref="CheckExistsByNameAsync"/>, in order.</summary>
    public List<FileName> CheckedNames { get; } = [];

    public Task<Exceptional<Option<FileEntity>>> TryGetByNameAsync(FileName name, CancellationToken cancellationToken = default) =>
        Task.FromResult<Exceptional<Option<FileEntity>>>(Option<FileEntity>.None.Instance);

    public Task<Exceptional<IReadOnlyList<FileName>>> GetExistingNamesAsync(IReadOnlyCollection<FileName> names, CancellationToken cancellationToken = default)
    {
        ExistingNamesQueryCount++;
        CheckedNames.AddRange(names);

        Exceptional<IReadOnlyList<FileName>> result = ExistsResult switch
        {
            Success<bool>(true) => names.ToList(),
            Success<bool> => ExistingNames.Where(stored => names.Any(name => string.Equals(name.Value, stored.Value, StringComparison.OrdinalIgnoreCase))).ToList(),
            Failure<bool> failure => failure.Exception,
            _ => throw new InvalidOperationException("Unexpected exceptional type.")
        };

        return Task.FromResult(result);
    }

    public Task<Exceptional<bool>> CheckExistsByNameAsync(FileName name, CancellationToken cancellationToken = default)
    {
        CheckedNames.Add(name);

        return Task.FromResult(ExistsResult);
    }
}
