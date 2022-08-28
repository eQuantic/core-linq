using System.Collections;

namespace eQuantic.Linq.Extensions;

/// <summary>
/// Enumerable Extensions
/// </summary>
public static class EnumerableExtensions
{
    private static readonly Type EnumerableType = typeof(Enumerable);

    /// <summary>
    /// Distinct by criteria.
    /// </summary>
    /// <typeparam name="TSource">The type of the source.</typeparam>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="source">The source.</param>
    /// <param name="keySelector">The key selector.</param>
    /// <returns></returns>
    public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
    {
        var knownKeys = new HashSet<TKey>();
        foreach (var element in source)
        {
            if (knownKeys.Add(keySelector(element)))
            {
                yield return element;
            }
        }
    }

    /// <summary>
    /// Invoke an action for each element
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="enumeration">The enumeration.</param>
    /// <param name="action">The action.</param>
    public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
    {
        if (enumeration == null || action == null)
        {
            return;
        }
        foreach (var item in enumeration)
        {
            action(item);
        }
    }

    /// <summary>
    /// Cast as type
    /// </summary>
    /// <param name="source"></param>
    /// <param name="targetType"></param>
    /// <returns></returns>
    public static IEnumerable CastAsType(this IEnumerable source, Type targetType)
    {
        var castMethod = EnumerableType.GetMethod(nameof(Enumerable.Cast)).MakeGenericMethod(targetType);

        return (IEnumerable)ExceptionHandlingInvoke(castMethod, null, new object[] { source });
    }

    /// <summary>
    /// To list of type
    /// </summary>
    /// <param name="source"></param>
    /// <param name="targetType"></param>
    /// <returns></returns>
    public static IList ToListOfType(this IEnumerable source, Type targetType)
    {
        var enumerable = CastAsType(source, targetType);

        var listMethod = EnumerableType.GetMethod(nameof(Enumerable.ToList)).MakeGenericMethod(targetType);

        return (IList)ExceptionHandlingInvoke(listMethod, null, new object[] { enumerable });
    }

    private static object ExceptionHandlingInvoke(System.Reflection.MethodInfo methodInfo, object obj, object[] parameters)
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