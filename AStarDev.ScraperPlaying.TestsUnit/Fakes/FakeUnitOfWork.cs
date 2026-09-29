using AStarDev.ControlDb;

namespace AStarDev.ScraperPlaying.TestsUnit.Fakes;

/// <summary>A unit of work that hands out registered repositories and counts how many times it was saved.</summary>
internal sealed class FakeUnitOfWork : IUnitOfWork
{
    private readonly Dictionary<Type, object> repositories = [];

    /// <summary>The ordered log of repository operations and saves (<c>add</c>, <c>delete</c>, <c>save</c>) shared with the repositories created by <see cref="Register{TAggregate, TKey}"/>.</summary>
    public List<string> Operations { get; } = [];

    /// <summary>The number of times <see cref="SaveChangesAsync"/> was called.</summary>
    public int SaveCount { get; private set; }

    /// <summary>Creates a repository whose operations are logged in <see cref="Operations"/> and makes it available to <see cref="GetRepository{TAggregate, TKey}"/>.</summary>
    public FakeRepository<TAggregate, TKey> Register<TAggregate, TKey>()
        where TAggregate : IAggregateRoot
    {
        var repository = new FakeRepository<TAggregate, TKey>(Operations);
        repositories[typeof(TAggregate)] = repository;

        return repository;
    }

    public IRepository<TAggregate, TKey> GetRepository<TAggregate, TKey>()
        where TAggregate : IAggregateRoot =>
        (IRepository<TAggregate, TKey>)repositories[typeof(TAggregate)];

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        Operations.Add("save");

        return Task.FromResult(1);
    }
}
