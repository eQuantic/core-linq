using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using eQuantic.Linq.Caching;

namespace eQuantic.Linq.Sorter;

/// <summary>
/// Enables asynchronous sorting of entities with expression caching for improved performance.
/// </summary>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
public static class AsyncEntitySorter<TEntity>
{
    private static readonly IExpressionCache Cache = new ExpressionCache();

    /// <summary>
    /// Creates an async entity sorter that orders by the specified property.
    /// </summary>
    /// <typeparam name="TKey">The type of the property to order by.</typeparam>
    /// <param name="keySelector">The property selector expression.</param>
    /// <returns>A new async entity sorter.</returns>
    public static IAsyncEntitySorter<TEntity> OrderBy<TKey>(Expression<Func<TEntity, TKey>> keySelector)
    {
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));
        return new OrderByAsyncSorter<TKey>(keySelector, SortDirection.Ascending);
    }

    /// <summary>
    /// Creates an async entity sorter that orders by the specified property in descending order.
    /// </summary>
    /// <typeparam name="TKey">The type of the property to order by.</typeparam>
    /// <param name="keySelector">The property selector expression.</param>
    /// <returns>A new async entity sorter.</returns>
    public static IAsyncEntitySorter<TEntity> OrderByDescending<TKey>(Expression<Func<TEntity, TKey>> keySelector)
    {
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));
        return new OrderByAsyncSorter<TKey>(keySelector, SortDirection.Descending);
    }

    /// <summary>
    /// Creates an async entity sorter that orders by the specified property name.
    /// </summary>
    /// <param name="propertyName">The property name to order by.</param>
    /// <returns>A new async entity sorter.</returns>
    public static IAsyncEntitySorter<TEntity> OrderBy(string propertyName)
    {
#if NET8_0_OR_GREATER
        // Check null first to maintain test compatibility
        if (propertyName == null)
            throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
#else
        if (string.IsNullOrEmpty(propertyName))
            throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));
#endif
        return OrderBy(propertyName, SortDirection.Ascending);
    }

    /// <summary>
    /// Creates an async entity sorter that orders by the specified property name in descending order.
    /// </summary>
    /// <param name="propertyName">The property name to order by.</param>
    /// <returns>A new async entity sorter.</returns>
    public static IAsyncEntitySorter<TEntity> OrderByDescending(string propertyName)
    {
#if NET8_0_OR_GREATER
        // Check null first to maintain test compatibility
        if (propertyName == null)
            throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
#else
        if (string.IsNullOrEmpty(propertyName))
            throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));
#endif
        return OrderBy(propertyName, SortDirection.Descending);
    }

    /// <summary>
    /// Creates an async entity sorter based on sortings.
    /// </summary>
    /// <param name="useNullCheckForNestedProperties">Whether to use null checks for nested properties.</param>
    /// <param name="sortings">The sortings to apply.</param>
    /// <returns>A new async entity sorter.</returns>
    public static IAsyncEntitySorter<TEntity> OrderBy(bool useNullCheckForNestedProperties, params ISorting[] sortings)
    {
        if (sortings == null || sortings.Length == 0)
            return new EmptyAsyncEntitySorter();

        return new CompositeSortingAsyncSorter(sortings, useNullCheckForNestedProperties);
    }

    private static IAsyncEntitySorter<TEntity> OrderBy(string propertyName, SortDirection direction)
    {
        // For simplicity, use the synchronous version wrapped
        return new CompositeSortingAsyncSorter(new[] { new Sorting(propertyName, direction) }, false);
    }

    private static Expression<Func<TEntity, object>> BuildKeySelectorExpression(string propertyName)
    {
        var parameter = Expression.Parameter(typeof(TEntity), "x");
        var property = Expression.Property(parameter, propertyName);
        var convert = Expression.Convert(property, typeof(object));
        return Expression.Lambda<Func<TEntity, object>>(convert, parameter);
    }

    /// <summary>
    /// Gets the shared expression cache instance.
    /// </summary>
    public static IExpressionCache ExpressionCache => Cache;

    private sealed class EmptyAsyncEntitySorter : IAsyncEntitySorter<TEntity>
    {
        public IOrderedQueryable<TEntity> Sort(IQueryable<TEntity> collection)
        {
            return collection.OrderBy(_ => 0);
        }

        public async Task<TEntity[]> SortAsync(IQueryable<TEntity> collection, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => collection.ToArray(), cancellationToken).ConfigureAwait(false);
        }

        public async IAsyncEnumerable<TEntity> SortAsyncEnumerable(IQueryable<TEntity> collection, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            foreach (var entity in collection)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return entity;
            }
        }

        public async Task<TEntity[]> SortPageAsync(IQueryable<TEntity> collection, int skip, int take, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => collection.Skip(skip).Take(take).ToArray(), cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class OrderByAsyncSorter<TKey> : IAsyncEntitySorter<TEntity>
    {
        private readonly Expression<Func<TEntity, TKey>> _keySelector;
        private readonly SortDirection _direction;
        private readonly string _cacheKey;

        public OrderByAsyncSorter(Expression<Func<TEntity, TKey>> keySelector, SortDirection direction)
        {
            _keySelector = keySelector;
            _direction = direction;
            _cacheKey = $"AsyncSorter_{typeof(TEntity).Name}_{typeof(TKey).Name}_{direction}_{keySelector}";
        }

        public IOrderedQueryable<TEntity> Sort(IQueryable<TEntity> collection)
        {
            return _direction == SortDirection.Ascending 
                ? collection.OrderBy(_keySelector)
                : collection.OrderByDescending(_keySelector);
        }

        public async Task<TEntity[]> SortAsync(IQueryable<TEntity> collection, CancellationToken cancellationToken = default)
        {
            // Use cache to get compiled key selector for better performance
            var compiledKeySelector = Cache.GetOrCreate(_cacheKey, () => _keySelector);
            var sorted = _direction == SortDirection.Ascending 
                ? collection.OrderBy(compiledKeySelector)
                : collection.OrderByDescending(compiledKeySelector);
            return await Task.Run(() => sorted.ToArray(), cancellationToken).ConfigureAwait(false);
        }

        public async IAsyncEnumerable<TEntity> SortAsyncEnumerable(IQueryable<TEntity> collection, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var compiledKeySelector = Cache.GetOrCreate(_cacheKey, () => _keySelector);
            var sorted = _direction == SortDirection.Ascending 
                ? collection.OrderBy(compiledKeySelector)
                : collection.OrderByDescending(compiledKeySelector);
            await Task.Yield();
            foreach (var entity in sorted)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return entity;
            }
        }

        public async Task<TEntity[]> SortPageAsync(IQueryable<TEntity> collection, int skip, int take, CancellationToken cancellationToken = default)
        {
            var compiledKeySelector = Cache.GetOrCreate(_cacheKey, () => _keySelector);
            var sorted = _direction == SortDirection.Ascending 
                ? collection.OrderBy(compiledKeySelector)
                : collection.OrderByDescending(compiledKeySelector);
            return await Task.Run(() => sorted.Skip(skip).Take(take).ToArray(), cancellationToken).ConfigureAwait(false);
        }
    }


    private sealed class CompositeSortingAsyncSorter : IAsyncEntitySorter<TEntity>
    {
        private readonly ISorting[] _sortings;
        private readonly bool _useNullCheckForNestedProperties;

        public CompositeSortingAsyncSorter(ISorting[] sortings, bool useNullCheckForNestedProperties)
        {
            _sortings = sortings;
            _useNullCheckForNestedProperties = useNullCheckForNestedProperties;
        }

        public IOrderedQueryable<TEntity> Sort(IQueryable<TEntity> collection)
        {
            return EntitySorter<TEntity>.OrderBy(_useNullCheckForNestedProperties, _sortings).Sort(collection);
        }

        public async Task<TEntity[]> SortAsync(IQueryable<TEntity> collection, CancellationToken cancellationToken = default)
        {
            var sorted = Sort(collection);
            return await Task.Run(() => sorted.ToArray(), cancellationToken).ConfigureAwait(false);
        }

        public async IAsyncEnumerable<TEntity> SortAsyncEnumerable(IQueryable<TEntity> collection, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var sorted = Sort(collection);
            await Task.Yield();
            foreach (var entity in sorted)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return entity;
            }
        }

        public async Task<TEntity[]> SortPageAsync(IQueryable<TEntity> collection, int skip, int take, CancellationToken cancellationToken = default)
        {
            var sorted = Sort(collection);
            return await Task.Run(() => sorted.Skip(skip).Take(take).ToArray(), cancellationToken).ConfigureAwait(false);
        }
    }
}