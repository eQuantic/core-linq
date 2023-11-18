using System.Linq.Expressions;
using eQuantic.Linq.Casting;
using eQuantic.Linq.Extensions;
using eQuantic.Linq.Sorter.Casting;

namespace eQuantic.Linq.Sorter.Extensions;

public static class SortingExtensions
{
    public static Sorting<TEntity>[] Cast<TEntity>(this ISorting[] sorting,
        Action<SortingCastOptions<TEntity>> options)
    {
        var list = new List<Sorting<TEntity>>();
        var castOptions = new SortingCastOptions<TEntity>();
        options.Invoke(castOptions);

        ThrowUnmapped(sorting, castOptions);

        foreach (var sortingItem in sorting)
        {
            list.AddRange(Cast(sortingItem, castOptions));
        }

        return list.ToArray();
    }

    public static Sorting<TEntity>[] Cast<TEntity>(this ISorting sorting,
        Action<SortingCastOptions<TEntity>> options)
    {
        var castOptions = new SortingCastOptions<TEntity>();
        options.Invoke(castOptions);

        ThrowUnmapped(new[] { sorting }, castOptions);

        return Cast(sorting, castOptions).ToArray();
    }

    public static Sorting<TEntity>[] Merge<TEntity>(this Sorting<TEntity>[] sorting, Sorting<TEntity>[] source)
    {
        var list = sorting.ToList();
        list.AddRange(sorting.Where(item => source.All(s => s.ColumnName != item.ColumnName)));
        return list.ToArray();
    }

    private static List<Sorting<TEntity>> Cast<TEntity>(ISorting sortingItem, SortingCastOptions<TEntity> options)
    {
        var list = new List<Sorting<TEntity>>();
        var mapping = options.GetMapping();
        var excludeUnmapped = options.GetExcludeUnmapped();
        var excluded = options.GetExcluded();
        var useColumnFallback = options.GetUseColumnFallback();
        var columnFallbackApplicability = options.GetColumnFallbackApplicability();

        if (excluded.Any(o => sortingItem.ColumnName.Equals(o, StringComparison.InvariantCultureIgnoreCase)))
            return list;
        
        if (!excludeUnmapped && !mapping.Any(m =>
                m.Key.Equals(sortingItem.ColumnName, StringComparison.InvariantCultureIgnoreCase)))
        {
            var exp = sortingItem.ColumnName
                .GetColumnExpression<TEntity>(useColumnFallback &&
                                              columnFallbackApplicability == ColumnFallbackApplicability.FromSource)
                .ToExpFunc<TEntity>();
            list.Add(new Sorting<TEntity>(exp, sortingItem.SortDirection,
                columnFallbackApplicability == ColumnFallbackApplicability.ToDestination));
        }

        if (!mapping.ContainsKey(sortingItem.ColumnName))
        {
            return list;
        }

        var map = mapping[sortingItem.ColumnName];

        if (map.CustomSorting != null)
        {
            list.AddRange(map.CustomSorting(sortingItem));
        }
        else
        {
            list.Add(new Sorting<TEntity>(
                map.Column,
                map.Direction ?? sortingItem.SortDirection));
        }

        return list;
    }

    private static void ThrowUnmapped<TEntity>(IEnumerable<ISorting> sorting, SortingCastOptions<TEntity> options)
    {
        var throwUnmapped = options.GetThrowUnmapped();
        var mapping = options.GetMapping();
        var mappedColumnNames = mapping.Keys.ToArray();
        var unmappedColumnNames = sorting
            .Select(o => o.ColumnName)
            .Except(mappedColumnNames, StringComparer.InvariantCultureIgnoreCase)
            .ToArray();

        if (throwUnmapped && unmappedColumnNames.Any())
        {
            throw new InvalidCastException(
                $"The following columns are unknown: {string.Join(", ", unmappedColumnNames)}");
        }
    }
}