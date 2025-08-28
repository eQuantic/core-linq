using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace eQuantic.Linq.Caching;

/// <summary>
/// Thread-safe expression cache implementation using ConcurrentDictionary
/// </summary>
public sealed class ExpressionCache : IExpressionCache
{
    private readonly ConcurrentDictionary<string, object> _cache = new();
    private int _hitCount;
    private int _missCount;

    /// <inheritdoc />
    public TDelegate GetOrCreate<TDelegate>(string key, Func<Expression<TDelegate>> expressionFactory) 
        where TDelegate : Delegate
    {
#if NET8_0_OR_GREATER
        // Check null first to maintain test compatibility
        if (key == null)
            throw new ArgumentException("Key cannot be null or empty", nameof(key));
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
#else
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));
#endif
        if (expressionFactory == null)
            throw new ArgumentNullException(nameof(expressionFactory));

        if (_cache.TryGetValue(key, out var cachedDelegate))
        {
            Interlocked.Increment(ref _hitCount);
            return (TDelegate)cachedDelegate;
        }

        var expression = expressionFactory();
        var compiledDelegate = expression.Compile();
        
        _cache.TryAdd(key, compiledDelegate);
        Interlocked.Increment(ref _missCount);
        
        return compiledDelegate;
    }

    /// <inheritdoc />
    public void Clear()
    {
        _cache.Clear();
        Interlocked.Exchange(ref _hitCount, 0);
        Interlocked.Exchange(ref _missCount, 0);
    }

    /// <inheritdoc />
    public CacheStatistics GetStatistics()
    {
        var hits = _hitCount;
        var misses = _missCount;
        var total = hits + misses;
        var hitRatio = total > 0 ? (double)hits / total : 0.0;

        return new CacheStatistics(hits, misses, _cache.Count, hitRatio);
    }

    /// <summary>
    /// Creates a cache key from multiple components
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CreateKey(params string[] components)
    {
        return string.Join("|", components);
    }

    /// <summary>
    /// Creates a cache key for a filter expression
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CreateFilterKey<T>(string columnName, string value, string operatorName)
    {
        return CreateKey("filter", typeof(T).FullName!, columnName, operatorName, value);
    }

    /// <summary>
    /// Creates a cache key for a sorting expression
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string CreateSortKey<T>(string columnName, string direction)
    {
        return CreateKey("sort", typeof(T).FullName!, columnName, direction);
    }
}