namespace eQuantic.Linq.Sorter;

/// <summary>
/// Specifies asynchronous methods for sorting collections.
/// </summary>
/// <typeparam name="TEntity">The element type of the collection to sort.</typeparam>
public interface IAsyncEntitySorter<TEntity> : IEntitySorter<TEntity>
{
    /// <summary>
    /// Asynchronously sorts the specified collection.
    /// </summary>
    /// <param name="collection">The collection to sort.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task containing the sorted collection as an array.</returns>
    Task<TEntity[]> SortAsync(IQueryable<TEntity> collection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns an async enumerable that yields sorted results.
    /// </summary>
    /// <param name="collection">The collection to sort.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of sorted entities.</returns>
    IAsyncEnumerable<TEntity> SortAsyncEnumerable(IQueryable<TEntity> collection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously gets a page of sorted results.
    /// </summary>
    /// <param name="collection">The collection to sort.</param>
    /// <param name="skip">Number of entities to skip.</param>
    /// <param name="take">Number of entities to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task containing the paged results.</returns>
    Task<TEntity[]> SortPageAsync(IQueryable<TEntity> collection, int skip, int take, CancellationToken cancellationToken = default);
}