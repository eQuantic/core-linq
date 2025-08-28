using System.Runtime.CompilerServices;
using eQuantic.Linq.Filter;
using eQuantic.Linq.Sorter;

namespace eQuantic.Linq.Extensions;

/// <summary>
/// Queryable Extensions with synchronous and asynchronous support
/// </summary>
public static class QueryableExtensions
{
    #region Synchronous Extensions

    /// <summary>
    /// Order by criteria using Sorting.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">The source.</param>
    /// <param name="sortings">The sortings.</param>
    /// <returns></returns>
    public static IOrderedQueryable<T> OrderByWithNullCheck<T>(this IQueryable<T> source, params ISorting[] sortings)
    {
        return EntitySorter<T>.OrderBy(true, sortings).Sort(source);
    }
    
    /// <summary>
    /// Order by criteria using Sorting.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">The source.</param>
    /// <param name="sortings">The sortings.</param>
    /// <returns></returns>
    public static IQueryable<T> OrderBy<T>(this IQueryable<T> source, params ISorting[] sortings)
    {
        return EntitySorter<T>.OrderBy(false, sortings).Sort(source);
    }

    /// <summary>
    /// Query by criteria using Filtering.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">The source.</param>
    /// <param name="filterings">The filterings.</param>
    /// <returns></returns>
    public static IQueryable<T> Where<T>(this IQueryable<T> source, params IFiltering[] filterings)
    {
        return EntityFilter<T>.Where(filterings).Filter(source);
    }

    #endregion

    #region Asynchronous Extensions

    /// <summary>
    /// Asynchronously filters and returns results as an array using the specified filterings.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The source queryable.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="filterings">The filterings to apply.</param>
    /// <returns>A task containing the filtered results as an array.</returns>
    public static async Task<T[]> WhereAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default, params IFiltering[] filterings)
    {
        var filter = AsyncEntityFilter<T>.Where(filterings);
        return await filter.FilterAsync(source, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously filters and returns results as an async enumerable using the specified filterings.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The source queryable.</param>
    /// <param name="filterings">The filterings to apply.</param>
    /// <returns>An async enumerable of filtered results.</returns>
    public static IAsyncEnumerable<T> WhereAsyncEnumerable<T>(this IQueryable<T> source, params IFiltering[] filterings)
    {
        var filter = AsyncEntityFilter<T>.Where(filterings);
        return filter.FilterAsyncEnumerable(source);
    }

    /// <summary>
    /// Asynchronously sorts and returns results as an array using the specified sortings.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The source queryable.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="sortings">The sortings to apply.</param>
    /// <returns>A task containing the sorted results as an array.</returns>
    public static async Task<T[]> OrderByAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default, params ISorting[] sortings)
    {
        var sorter = AsyncEntitySorter<T>.OrderBy(false, sortings);
        return await sorter.SortAsync(source, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously sorts with null checking and returns results as an array using the specified sortings.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The source queryable.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="sortings">The sortings to apply.</param>
    /// <returns>A task containing the sorted results as an array.</returns>
    public static async Task<T[]> OrderByWithNullCheckAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default, params ISorting[] sortings)
    {
        var sorter = AsyncEntitySorter<T>.OrderBy(true, sortings);
        return await sorter.SortAsync(source, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously sorts and returns results as an async enumerable using the specified sortings.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The source queryable.</param>
    /// <param name="sortings">The sortings to apply.</param>
    /// <returns>An async enumerable of sorted results.</returns>
    public static IAsyncEnumerable<T> OrderByAsyncEnumerable<T>(this IQueryable<T> source, params ISorting[] sortings)
    {
        var sorter = AsyncEntitySorter<T>.OrderBy(false, sortings);
        return sorter.SortAsyncEnumerable(source);
    }

    /// <summary>
    /// Asynchronously filters, sorts, and returns paginated results.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The source queryable.</param>
    /// <param name="skip">Number of items to skip.</param>
    /// <param name="take">Number of items to take.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="filterings">The filterings to apply.</param>
    /// <param name="sortings">The sortings to apply.</param>
    /// <returns>A task containing the paginated results.</returns>
    public static async Task<T[]> FilterSortPageAsync<T>(
        this IQueryable<T> source, 
        int skip, 
        int take, 
        CancellationToken cancellationToken = default,
        IFiltering[]? filterings = null, 
        ISorting[]? sortings = null)
    {
        var query = source;

        if (filterings is { Length: > 0 })
        {
            var filter = AsyncEntityFilter<T>.Where(filterings);
            query = filter.Filter(query);
        }

        if (sortings is { Length: > 0 })
        {
            var sorter = AsyncEntitySorter<T>.OrderBy(false, sortings);
            return await sorter.SortPageAsync(query, skip, take, cancellationToken).ConfigureAwait(false);
        }

        return await Task.Run(() => query.Skip(skip).Take(take).ToArray(), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously gets the first entity that matches the specified filterings, or null if none found.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The source queryable.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="filterings">The filterings to apply.</param>
    /// <returns>The first matching entity or null.</returns>
    public static async Task<T?> FirstOrDefaultAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default, params IFiltering[] filterings)
    {
        var filter = AsyncEntityFilter<T>.Where(filterings);
        return await filter.FirstOrDefaultAsync(source, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously counts the entities that match the specified filterings.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The source queryable.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="filterings">The filterings to apply.</param>
    /// <returns>The count of matching entities.</returns>
    public static async Task<int> CountAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default, params IFiltering[] filterings)
    {
        var filter = AsyncEntityFilter<T>.Where(filterings);
        return await filter.CountAsync(source, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously checks if any entity matches the specified filterings.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The source queryable.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="filterings">The filterings to apply.</param>
    /// <returns>True if any entity matches, false otherwise.</returns>
    public static async Task<bool> AnyAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default, params IFiltering[] filterings)
    {
        var filter = AsyncEntityFilter<T>.Where(filterings);
        return await filter.AnyAsync(source, cancellationToken).ConfigureAwait(false);
    }

    #endregion
}