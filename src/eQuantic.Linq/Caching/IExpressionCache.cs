using System.Linq.Expressions;

namespace eQuantic.Linq.Caching;

/// <summary>
/// Provides caching functionality for compiled expressions to improve performance
/// </summary>
public interface IExpressionCache
{
    /// <summary>
    /// Gets or creates a cached compiled expression
    /// </summary>
    /// <typeparam name="TDelegate">The delegate type</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="expressionFactory">Factory to create the expression if not cached</param>
    /// <returns>Compiled delegate</returns>
    TDelegate GetOrCreate<TDelegate>(string key, Func<Expression<TDelegate>> expressionFactory) 
        where TDelegate : Delegate;

    /// <summary>
    /// Clears the cache
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets cache statistics
    /// </summary>
    CacheStatistics GetStatistics();
}

/// <summary>
/// Cache performance statistics
/// </summary>
public class CacheStatistics
{
    public CacheStatistics(int hitCount, int missCount, int totalEntries, double hitRatio)
    {
        HitCount = hitCount;
        MissCount = missCount;
        TotalEntries = totalEntries;
        HitRatio = hitRatio;
    }

    public int HitCount { get; }
    public int MissCount { get; }
    public int TotalEntries { get; }
    public double HitRatio { get; }
}