namespace AStarDev.ControlDb;

/// <summary>Defines the contract for a unit of work, which encapsulates a set of operations to be performed as a single transaction.</summary>
public interface IUnitOfWork
{
    /// <summary>Gets the repository for the specified aggregate type and key type.</summary>
    /// <typeparam name="TAggregate">The type of the aggregate root.</typeparam>
    /// <typeparam name="TKey">The type of the key for the aggregate root.
    /// Ideally, this should be a strongly-typed identifier.
    /// </typeparam>
    /// <returns>The repository for the specified aggregate type and key type.</returns>
    /// <example>
    /// var repository = unitOfWork.GetRepository<MyAggregate, Guid>();
    /// var repository = unitOfWork.GetRepository<MyAggregate, StrongId<Guid>>();
    /// </example>
    IRepository<TAggregate, TKey> GetRepository<TAggregate, TKey>() where TAggregate : IAggregateRoot;

    /// <summary>Saves all changes made in this unit of work to the underlying database.</summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Stops tracking every entity this unit of work has loaded or added, discarding any unsaved changes. Saved rows are unaffected.</summary>
    void ClearChangeTracker();

    /// <summary>Runs <paramref name="work"/> in a single database transaction: everything saved during it is committed together, or rolled back if it throws.</summary>
    /// <param name="work">The work to run; it may call <see cref="SaveChangesAsync"/> more than once.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InTransactionAsync(Func<Task> work, CancellationToken cancellationToken = default);
}
