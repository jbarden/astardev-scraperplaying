using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.TestsUnit.Fakes;

/// <summary>A files query whose existence result is set by the test and which records the names it was asked about.</summary>
internal sealed class FakeFilesQuery : IFilesQuery
{
    /// <summary>The result of <see cref="CheckExistsByNameAsync"/>.</summary>
    public Exceptional<bool> ExistsResult { get; set; } = false;

    /// <summary>The names passed to <see cref="CheckExistsByNameAsync"/>, in order.</summary>
    public List<FileName> CheckedNames { get; } = [];

    public Task<Exceptional<Option<FileEntity>>> TryGetByNameAsync(FileName name, CancellationToken cancellationToken = default) =>
        Task.FromResult<Exceptional<Option<FileEntity>>>(Option<FileEntity>.None.Instance);

    public Task<Exceptional<bool>> CheckExistsByNameAsync(FileName name, CancellationToken cancellationToken = default)
    {
        CheckedNames.Add(name);

        return Task.FromResult(ExistsResult);
    }
}
