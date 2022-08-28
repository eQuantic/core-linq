using eQuantic.Linq.Filter;
using eQuantic.Linq.Sorter;

namespace eQuantic.Linq.Extensions;

/// <summary>
/// Queryable Extensions
/// </summary>
public static class QueryableExtensions
{
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
}