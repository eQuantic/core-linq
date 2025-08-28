using System.Collections;
using eQuantic.Linq.Casting;

namespace eQuantic.Linq.Extensions;

/// <summary>
/// Enumerable Extensions
/// </summary>
public static class EnumerableExtensions
{
    private static readonly Type EnumerableType = typeof(Enumerable);


    /// <summary>
    /// Invoke an action for each element using modern null checking.
    /// </summary>
    /// <typeparam name="T">The type of elements in the enumeration.</typeparam>
    /// <param name="enumeration">The enumeration to iterate over.</param>
    /// <param name="action">The action to invoke for each element.</param>
    public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(enumeration);
        ArgumentNullException.ThrowIfNull(action);

        foreach (var item in enumeration)
        {
            action(item);
        }
    }

    /// <summary>
    /// Cast as type using modern C# patterns.
    /// </summary>
    /// <param name="source">The source enumerable to cast.</param>
    /// <param name="targetType">The target type to cast to.</param>
    /// <returns>An enumerable of the target type.</returns>
    public static IEnumerable CastAsType(this IEnumerable source, Type targetType)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(targetType);

        var castMethod = CastingCache.GetOrCacheMethod(EnumerableType, nameof(Enumerable.Cast), targetType);
        return castMethod switch
        {
            not null => (IEnumerable)ExceptionHandlingInvoke(castMethod, null, [source]),
            null => throw new InvalidOperationException($"Unable to create cast method for type {targetType.Name}")
        };
    }

    /// <summary>
    /// Convert enumerable to a list of specified type using modern patterns.
    /// </summary>
    /// <param name="source">The source enumerable to convert.</param>
    /// <param name="targetType">The target type for the list elements.</param>
    /// <returns>A list of the target type.</returns>
    public static IList ToListOfType(this IEnumerable source, Type targetType)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(targetType);

        var enumerable = CastAsType(source, targetType);
        var listMethod = CastingCache.GetOrCacheMethod(EnumerableType, nameof(Enumerable.ToList), targetType);

        return listMethod switch
        {
            not null => (IList)ExceptionHandlingInvoke(listMethod, null, [enumerable]),
            null => throw new InvalidOperationException($"Unable to create ToList method for type {targetType.Name}")
        };
    }

    private static object ExceptionHandlingInvoke(System.Reflection.MethodInfo methodInfo, object? obj, object[] parameters)
    {
        try
        {
            return methodInfo.Invoke(obj, parameters);
        }
        catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }
}
