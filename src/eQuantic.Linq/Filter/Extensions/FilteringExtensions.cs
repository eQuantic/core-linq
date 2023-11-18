using System.Linq.Expressions;
using eQuantic.Linq.Casting;
using eQuantic.Linq.Extensions;
using eQuantic.Linq.Filter.Casting;

namespace eQuantic.Linq.Filter.Extensions;

public static class FilteringExtensions
{
    public static IFiltering<TEntity>[] Cast<TEntity>(this IFiltering[] filtering,
        Action<FilteringCastOptions<TEntity>> options)
    {
        var list = new List<IFiltering<TEntity>>();
        var castOptions = new FilteringCastOptions<TEntity>();
        options.Invoke(castOptions);

        ThrowUnmapped(filtering, castOptions);

        foreach (var filteringItem in filtering)
        {
            list.AddRange(Cast(filteringItem, castOptions));
        }

        return list.ToArray();
    }

    public static IFiltering<TEntity>[] Cast<TEntity>(this IFiltering filtering,
        Action<FilteringCastOptions<TEntity>> options)
    {
        var castOptions = new FilteringCastOptions<TEntity>();
        options.Invoke(castOptions);

        ThrowUnmapped(new[] { filtering }, castOptions);

        return Cast(filtering, castOptions).ToArray();
    }

    public static IFiltering<TEntity>[] Merge<TEntity>(this IFiltering<TEntity>[] filtering,
        IFiltering<TEntity>[] source)
    {
        var list = filtering.ToList();
        list.AddRange(filtering.Where(item => source.All(s => s.ColumnName != item.ColumnName)));
        return list.ToArray();
    }

    private static List<IFiltering<TEntity>> Cast<TEntity>(
        IFiltering filteringItem,
        FilteringCastOptions<TEntity> options)
    {
        var list = new List<IFiltering<TEntity>>();

        if (filteringItem is CompositeFiltering compositeFiltering)
        {
            var values = compositeFiltering.Values.SelectMany(f => Cast(f, options)).ToArray();
            list.Add(new CompositeFiltering<TEntity>(compositeFiltering.CompositeOperator, values));
            return list;
        }

        var mapping = options.GetMapping();
        var excludeUnmapped = options.GetExcludeUnmapped();
        var excluded = options.GetExcluded();
        var useColumnFallback = options.GetUseColumnFallback();
        var columnFallbackApplicability = options.GetColumnFallbackApplicability();

        if (excluded.Any(o => filteringItem.ColumnName.Equals(o, StringComparison.InvariantCultureIgnoreCase)))
            return list;

        if (!excludeUnmapped && !mapping.Any(m =>
                m.Key.Equals(filteringItem.ColumnName, StringComparison.InvariantCultureIgnoreCase)))
        {
            var exp = GetExpression<TEntity>(filteringItem.ColumnName, useColumnFallback, columnFallbackApplicability);

            list.Add(new Filtering<TEntity>(exp,
                filteringItem.StringValue,
                filteringItem.Operator,
                columnFallbackApplicability == ColumnFallbackApplicability.ToDestination));
        }

        if (!mapping.ContainsKey(filteringItem.ColumnName))
        {
            return list;
        }

        var map = mapping[filteringItem.ColumnName];

        if (map.CustomFiltering != null)
        {
            list.AddRange(map.CustomFiltering(filteringItem));
        }
        else
        {
            var exp = map.ColumnExpression;
            if (exp == null && !string.IsNullOrEmpty(map.ColumnName))
            {
                exp = GetExpression<TEntity>(map.ColumnName, useColumnFallback, columnFallbackApplicability);
            }
            
            if(exp != null)
            {
                list.Add(new Filtering<TEntity>(
                    exp,
                    map.SetValue?.Invoke(filteringItem.StringValue) ?? filteringItem.StringValue,
                    map.Operator ?? filteringItem.Operator));
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

    private static void ThrowUnmapped<TEntity>(IEnumerable<IFiltering> filtering, FilteringCastOptions<TEntity> options)
    {
        var throwUnmapped = options.GetThrowUnmapped();
        var mapping = options.GetMapping();
        var mappedColumnNames = mapping.Keys.ToArray();
        var unmappedColumnNames = filtering
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