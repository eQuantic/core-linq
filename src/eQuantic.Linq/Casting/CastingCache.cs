using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace eQuantic.Linq.Casting;

/// <summary>
/// Provides caching for compiled casting expressions and reflection operations.
/// </summary>
public static class CastingCache
{
    private static readonly ConcurrentDictionary<string, MethodInfo> MethodCache = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();
    private static readonly ConcurrentDictionary<string, Delegate> CompiledExpressionCache = new();

    /// <summary>
    /// Gets a method from cache or caches it if not present.
    /// </summary>
    /// <param name="type">The type containing the method.</param>
    /// <param name="methodName">The name of the method.</param>
    /// <param name="genericTypes">Optional generic type arguments.</param>
    /// <returns>The cached method info.</returns>
    public static MethodInfo? GetOrCacheMethod(Type type, string methodName, params Type[] genericTypes)
    {
        var key = CreateMethodKey(type, methodName, genericTypes);
        
        return MethodCache.GetOrAdd(key, _ =>
        {
            var method = type.GetMethod(methodName);
            if (method != null && genericTypes.Length > 0)
            {
                method = method.MakeGenericMethod(genericTypes);
            }
            return method!;
        });
    }

    /// <summary>
    /// Gets properties for a type from cache or caches them if not present.
    /// </summary>
    /// <param name="type">The type to get properties for.</param>
    /// <returns>Array of property infos.</returns>
    public static PropertyInfo[] GetOrCacheProperties(Type type)
    {
        return PropertyCache.GetOrAdd(type, t => t.GetProperties());
    }

    /// <summary>
    /// Gets or compiles and caches an expression.
    /// </summary>
    /// <typeparam name="TDelegate">The delegate type.</typeparam>
    /// <param name="expression">The expression to compile.</param>
    /// <param name="cacheKey">The cache key.</param>
    /// <returns>The compiled delegate.</returns>
    public static TDelegate GetOrCompileExpression<TDelegate>(
        Expression<TDelegate> expression, 
        string cacheKey) where TDelegate : Delegate
    {
        return (TDelegate)CompiledExpressionCache.GetOrAdd(cacheKey, _ => expression.Compile());
    }

    /// <summary>
    /// Clears all cached data. Use for testing or memory management.
    /// </summary>
    public static void ClearCache()
    {
        MethodCache.Clear();
        PropertyCache.Clear();
        CompiledExpressionCache.Clear();
    }

    /// <summary>
    /// Gets cache statistics for monitoring and debugging.
    /// </summary>
    /// <returns>A dictionary with cache statistics.</returns>
    public static Dictionary<string, int> GetCacheStatistics()
    {
        return new Dictionary<string, int>
        {
            ["MethodCache"] = MethodCache.Count,
            ["PropertyCache"] = PropertyCache.Count,
            ["CompiledExpressionCache"] = CompiledExpressionCache.Count
        };
    }

    private static string CreateMethodKey(Type type, string methodName, Type[] genericTypes)
    {
        var genericTypesKey = genericTypes.Length > 0 
            ? $"<{string.Join(",", genericTypes.Select(t => t.FullName))}>"
            : string.Empty;
            
        return $"{type.FullName}.{methodName}{genericTypesKey}";
    }
}