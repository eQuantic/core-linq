using System.Linq.Expressions;
using eQuantic.Linq.Casting;
using eQuantic.Linq.Extensions;
using eQuantic.Linq.Sorter.Casting;

namespace eQuantic.Linq.Sorter.Extensions;

public static class SortingExtensions
{
    /// <summary>
    /// Modern cast method using FluentCastBuilder for enhanced performance and type safety.
    /// </summary>
    /// <typeparam name="TEntity">The target entity type.</typeparam>
    /// <param name="sorting">The source sorting array.</param>
    /// <param name="builderAction">Action to configure the FluentCastBuilder.</param>
    /// <returns>Array of cast sorting items.</returns>
    public static Sorting<TEntity>[] CastWith<TEntity>(this ISorting[] sorting,
        Action<FluentCastBuilder<TEntity>> builderAction)
    {
        var builder = sorting.ToFluentBuilder<TEntity>();
        builderAction(builder);
        var options = builder.BuildSortingOptions();

        return Cast<TEntity>(sorting, castOptions => ConfigureFromBuilder(castOptions, options));
    }

    /// <summary>
    /// Modern cast method using FluentCastBuilder for enhanced performance and type safety.
    /// </summary>
    /// <typeparam name="TEntity">The target entity type.</typeparam>
    /// <param name="sorting">The source sorting item.</param>
    /// <param name="builderAction">Action to configure the FluentCastBuilder.</param>
    /// <returns>Array of cast sorting items.</returns>
    public static Sorting<TEntity>[] CastWith<TEntity>(this ISorting sorting,
        Action<FluentCastBuilder<TEntity>> builderAction)
    {
        return new[] { sorting }.CastWith(builderAction);
    }

    /// <summary>
    /// Helper method to configure legacy options from modern FluentCastBuilder.
    /// </summary>
    private static void ConfigureFromBuilder<TEntity>(SortingCastOptions<TEntity> targetOptions, SortingCastOptions<TEntity> sourceOptions)
    {
        // Copy configuration from the modern builder to legacy options
        var sourceMapping = sourceOptions.GetMapping();
        var sourceExcluded = sourceOptions.GetExcluded();

        foreach (var mapping in sourceMapping)
        {
            if (mapping.Value.ColumnExpression != null)
            {
                targetOptions.Map(mapping.Key, mapping.Value.ColumnExpression, mapping.Value.Direction);
            }
            else if (!string.IsNullOrEmpty(mapping.Value.ColumnName))
            {
                targetOptions.Map(mapping.Key, mapping.Value.ColumnName);
            }
        }

        foreach (var excluded in sourceExcluded)
        {
            targetOptions.Exclude(excluded);
        }

        if (sourceOptions.GetExcludeUnmapped())
            targetOptions.ExcludeUnmapped();

        if (sourceOptions.GetThrowUnmapped())
            targetOptions.ThrowUnmapped();

        if (sourceOptions.GetUseColumnFallback())
            targetOptions.UseColumnFallback(sourceOptions.GetColumnFallbackApplicability());
    }

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

        ThrowUnmapped([sorting], castOptions);

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
            var exp = GetExpression<TEntity>(sortingItem.ColumnName, useColumnFallback, columnFallbackApplicability);
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
            var exp = map.ColumnExpression;
            if (exp == null && !string.IsNullOrEmpty(map.ColumnName))
            {
                exp = GetExpression<TEntity>(map.ColumnName, useColumnFallback, columnFallbackApplicability);
            }

            if (exp != null)
            {
                list.Add(new Sorting<TEntity>(
                    exp,
                    map.Direction ?? sortingItem.SortDirection));
            }
        }

        return list;
    }

    private static Expression<Func<TEntity, object>> GetExpression<TEntity>(
        string columnName, bool useColumnFallback, ColumnFallbackApplicability columnFallbackApplicability)
    {
        var exp = columnName
            .GetColumnExpression<TEntity>(useColumnFallback &&
                                          columnFallbackApplicability == ColumnFallbackApplicability.FromSource)
            .ToExpFunc<TEntity>();
        return exp;
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
