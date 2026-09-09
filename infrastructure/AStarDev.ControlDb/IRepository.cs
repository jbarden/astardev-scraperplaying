using AStarDev.FunctionalParadigm;

namespace AStarDev.ControlDb;

/// <summary>
/// Represents a repository for managing aggregates of type <typeparamref name="TAggregate"/> with keys of type <typeparamref name="TKey"/>.
/// </summary>
public interface IRepository<TAggregate, TKey>
{
    /// <summary>
    /// Tries to find an aggregate by its key. Returns an exceptional result containing an option of the aggregate if found, or an empty option if not found.
    /// </summary>
    /// <param name="key">The key of the aggregate to find.</param>
    /// <returns>An exceptional result containing an option of the aggregate if found, or an empty option if not found.</returns>
    Task<Exceptional<Option<TAggregate>>> TryFindAsync(TKey key);

    /// <summary>
    /// Adds a new aggregate to the repository. Returns an exceptional result containing the added aggregate if successful.
    /// </summary>
    /// <param name="aggregate">The aggregate to add.</param>
    /// <returns>An exceptional result containing the added aggregate if successful.</returns>
    Exceptional<TAggregate> Add(TAggregate aggregate);

    /// <summary>
    /// Deletes an aggregate from the repository. Returns an exceptional result indicating the success or failure of the operation.
    /// </summary>
    /// <param name="aggregate">The aggregate to delete.</param>
    /// <returns>An exceptional result indicating the success or failure of the operation.</returns>
    Exceptional<UnitFp> Delete(TAggregate aggregate);

    /// <summary>
    /// Deletes an aggregate by its key from the repository. Returns an exceptional result indicating the success or failure of the operation.
    /// </summary>
    /// <param name="key">The key of the aggregate to delete.</param>
    /// <returns>An exceptional result indicating the success or failure of the operation.</returns>
    public async Task<Exceptional<UnitFp>> DeleteAsync(TKey key)
        => (await TryFindAsync(key)).Match(
            findById => findById.Match(
                aggregate => {
                    return Delete(aggregate);
                },
                () => UnitFp.Instance
            ),
            ex => ex
        );
}

public interface IUnitOfWork
{
    IRepository<TAggregate, TKey> Repository<TAggregate, TKey>() where TAggregate : class;
}