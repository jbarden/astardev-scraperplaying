using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.TestsUnit.Fakes;

/// <summary>A files query whose existence result is set by the test and which records the names it was asked about.</summary>
internal sealed class FakeFilesQuery : IFilesQuery
{
    /// <summary>The result of <see cref="CheckExistsByNameAsync"/>; when <c>true</c>, <see cref="GetExistingHandlesAsync"/> reports every supplied handle as existing, and when a failure, it returns that failure.</summary>
    public Exceptional<bool> ExistsResult { get; set; } = false;

    /// <summary>The handles <see cref="GetExistingHandlesAsync"/> reports as existing when <see cref="ExistsResult"/> is <c>false</c>; matched case-insensitively, like the database collation.</summary>
    public List<FileHandle> ExistingHandles { get; } = [];

    /// <summary>The number of times <see cref="GetExistingHandlesAsync"/> was called.</summary>
    public int ExistingHandlesQueryCount { get; private set; }

    /// <summary>The handles passed to <see cref="GetExistingHandlesAsync"/>, in order.</summary>
    public List<FileHandle> CheckedHandles { get; } = [];

    /// <summary>The names passed to <see cref="CheckExistsByNameAsync"/>, in order.</summary>
    public List<FileName> CheckedNames { get; } = [];

    public Task<Exceptional<Option<FileEntity>>> TryGetByNameAsync(FileName name, CancellationToken cancellationToken = default) =>
        Task.FromResult<Exceptional<Option<FileEntity>>>(Option<FileEntity>.None.Instance);

    public Task<Exceptional<IReadOnlyList<FileHandle>>> GetExistingHandlesAsync(IReadOnlyCollection<FileHandle> fileHandles, CancellationToken cancellationToken = default)
    {
        ExistingHandlesQueryCount++;
        CheckedHandles.AddRange(fileHandles);

        Exceptional<IReadOnlyList<FileHandle>> result = ExistsResult switch
        {
            Success<bool>(true) => fileHandles.ToList(),
            Success<bool> => ExistingHandles.Where(stored => fileHandles.Any(handle => string.Equals(handle.Value, stored.Value, StringComparison.OrdinalIgnoreCase))).ToList(),
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
