using System.Linq.Expressions;

namespace eQuantic.Linq.Filter;

/// <summary>
/// Specifies asynchronous methods that filter a collection by returning a filtered collection.
/// </summary>
/// <typeparam name="TEntity">The element type of the collection to filter.</typeparam>
public interface IAsyncEntityFilter<TEntity> : IEntityFilter<TEntity>
{
    /// <summary>
    /// Asynchronously filters the specified collection.
    /// </summary>
    /// <param name="collection">The collection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task containing the filtered collection as an array.</returns>
    Task<TEntity[]> FilterAsync(IQueryable<TEntity> collection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns an async enumerable that yields filtered results.
    /// </summary>
    /// <param name="collection">The collection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of filtered entities.</returns>
    IAsyncEnumerable<TEntity> FilterAsyncEnumerable(IQueryable<TEntity> collection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously gets the first entity that matches the filter, or null if none found.
    /// </summary>
    /// <param name="collection">The collection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The first matching entity or null.</returns>
    Task<TEntity?> FirstOrDefaultAsync(IQueryable<TEntity> collection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously counts the entities that match the filter.
    /// </summary>
    /// <param name="collection">The collection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of matching entities.</returns>
    Task<int> CountAsync(IQueryable<TEntity> collection, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously checks if any entity matches the filter.
    /// </summary>
    /// <param name="collection">The collection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if any entity matches, false otherwise.</returns>
    Task<bool> AnyAsync(IQueryable<TEntity> collection, CancellationToken cancellationToken = default);
}