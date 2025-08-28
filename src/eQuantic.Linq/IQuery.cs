using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using eQuantic.Linq.Filter;
using eQuantic.Linq.Sorter;

namespace eQuantic.Linq;

/// <summary>
/// Represents a fluent query builder for filtering and sorting operations.
/// </summary>
/// <typeparam name="TEntity">The type of entity to query.</typeparam>
public interface IQuery<TEntity> where TEntity : class
{
    /// <summary>
    /// Adds a where condition using AND logic.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>The current query instance for chaining.</returns>
    Query<TEntity> Where(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// Adds a where condition using AND logic with IFiltering.
    /// </summary>
    /// <param name="filtering">The filtering specification.</param>
    /// <returns>The current query instance for chaining.</returns>
    Query<TEntity> Where(IFiltering filtering);

    /// <summary>
    /// Adds multiple where conditions using AND logic with IFiltering array.
    /// </summary>
    /// <param name="filterings">The filtering specifications.</param>
    /// <returns>The current query instance for chaining.</returns>
    Query<TEntity> Where(params IFiltering[] filterings);

    /// <summary>
    /// Adds a condition using AND logic (alias for Where).
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>The current query instance for chaining.</returns>
    Query<TEntity> And(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// Adds a condition using OR logic.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>The current query instance for chaining.</returns>
    Query<TEntity> Or(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// Adds an ascending sort by the specified property.
    /// </summary>
    /// <typeparam name="TKey">The type of the sort key.</typeparam>
    /// <param name="keySelector">The property selector for sorting.</param>
    /// <returns>The current query instance for chaining.</returns>
    Query<TEntity> OrderBy<TKey>(Expression<Func<TEntity, TKey>> keySelector);

    /// <summary>
    /// Adds an ascending sort by the specified property name.
    /// </summary>
    /// <param name="propertyName">The name of the property to sort by.</param>
    /// <returns>The current query instance for chaining.</returns>
    Query<TEntity> OrderBy(string propertyName);

    /// <summary>
    /// Adds a descending sort by the specified property.
    /// </summary>
    /// <typeparam name="TKey">The type of the sort key.</typeparam>
    /// <param name="keySelector">The property selector for sorting.</param>
    /// <returns>The current query instance for chaining.</returns>
    Query<TEntity> OrderByDescending<TKey>(Expression<Func<TEntity, TKey>> keySelector);

    /// <summary>
    /// Adds a descending sort by the specified property name.
    /// </summary>
    /// <param name="propertyName">The name of the property to sort by.</param>
    /// <returns>The current query instance for chaining.</returns>
    Query<TEntity> OrderByDescending(string propertyName);

    /// <summary>
    /// Adds an additional ascending sort criterion.
    /// </summary>
    /// <typeparam name="TKey">The type of the sort key.</typeparam>
    /// <param name="keySelector">The property selector for sorting.</param>
    /// <returns>The current query instance for chaining.</returns>
    Query<TEntity> ThenBy<TKey>(Expression<Func<TEntity, TKey>> keySelector);

    /// <summary>
    /// Adds an additional ascending sort criterion by property name.
    /// </summary>
    /// <param name="propertyName">The name of the property to sort by.</param>
    /// <returns>The current query instance for chaining.</returns>
    Query<TEntity> ThenBy(string propertyName);

    /// <summary>
    /// Adds an additional descending sort criterion.
    /// </summary>
    /// <typeparam name="TKey">The type of the sort key.</typeparam>
    /// <param name="keySelector">The property selector for sorting.</param>
    /// <returns>The current query instance for chaining.</returns>
    Query<TEntity> ThenByDescending<TKey>(Expression<Func<TEntity, TKey>> keySelector);

    /// <summary>
    /// Adds an additional descending sort criterion by property name.
    /// </summary>
    /// <param name="propertyName">The name of the property to sort by.</param>
    /// <returns>The current query instance for chaining.</returns>
    Query<TEntity> ThenByDescending(string propertyName);

    /// <summary>
    /// Applies the query to the specified queryable collection.
    /// </summary>
    /// <param name="source">The source queryable collection.</param>
    /// <returns>The filtered and sorted queryable collection.</returns>
    IQueryable<TEntity> ApplyTo(IQueryable<TEntity> source);

    /// <summary>
    /// Applies the query asynchronously to the specified queryable collection.
    /// </summary>
    /// <param name="source">The source queryable collection.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The filtered and sorted array of entities.</returns>
    Task<TEntity[]> ApplyToAsync(IQueryable<TEntity> source, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies the query asynchronously with enumerable result.
    /// </summary>
    /// <param name="source">The source queryable collection.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An async enumerable of filtered and sorted entities.</returns>
    IAsyncEnumerable<TEntity> ApplyToAsyncEnumerable(IQueryable<TEntity> source, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the filter expression if filters are present.
    /// </summary>
    /// <returns>The combined filter expression, or null if no filters.</returns>
    Expression<Func<TEntity, bool>>? GetFilterExpression();

    /// <summary>
    /// Gets the sort expressions.
    /// </summary>
    /// <returns>Array of sorting specifications.</returns>
    ISorting[] GetSortExpressions();
}