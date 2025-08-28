using System.Linq.Expressions;
using eQuantic.Linq.Casting;
using eQuantic.Linq.Extensions;
using eQuantic.Linq.Filter.Casting;

namespace eQuantic.Linq.Filter.Extensions;

public static class FilteringExtensions
{
    /// <summary>
    /// Modern cast method using FluentCastBuilder for enhanced performance and type safety.
    /// </summary>
    /// <typeparam name="TEntity">The target entity type.</typeparam>
    /// <param name="filtering">The source filtering array.</param>
    /// <param name="builderAction">Action to configure the FluentCastBuilder.</param>
    /// <returns>Array of cast filtering items.</returns>
    public static IFiltering<TEntity>[] CastWith<TEntity>(this IFiltering[] filtering,
        Action<FluentCastBuilder<TEntity>> builderAction)
    {
        var builder = filtering.ToFluentBuilder<TEntity>();
        builderAction(builder);
        var options = builder.BuildFilteringOptions();

        return Cast<TEntity>(filtering, castOptions => ConfigureFromBuilder(castOptions, options));
    }

    /// <summary>
    /// Modern cast method using FluentCastBuilder for enhanced performance and type safety.
    /// </summary>
    /// <typeparam name="TEntity">The target entity type.</typeparam>
    /// <param name="filtering">The source filtering item.</param>
    /// <param name="builderAction">Action to configure the FluentCastBuilder.</param>
    /// <returns>Array of cast filtering items.</returns>
    public static IFiltering<TEntity>[] CastWith<TEntity>(this IFiltering filtering,
        Action<FluentCastBuilder<TEntity>> builderAction)
    {
        return new[] { filtering }.CastWith(builderAction);
    }

    /// <summary>
    /// Helper method to configure legacy options from modern FluentCastBuilder.
    /// </summary>
    private static void ConfigureFromBuilder<TEntity>(FilteringCastOptions<TEntity> targetOptions, FilteringCastOptions<TEntity> sourceOptions)
    {
        // Copy configuration from the modern builder to legacy options
        var sourceMapping = sourceOptions.GetMapping();
        var sourceExcluded = sourceOptions.GetExcluded();
        
        foreach (var mapping in sourceMapping)
        {
            if (mapping.Value.ColumnExpression != null)
            {
                targetOptions.Map(mapping.Key, mapping.Value.ColumnExpression, mapping.Value.SetValue, mapping.Value.Operator);
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

    /// <summary>
    /// Merges two filtering arrays, avoiding duplicates based on column names using modern LINQ.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="filtering">The primary filtering array.</param>
    /// <param name="source">The source filtering array to merge.</param>
    /// <returns>Merged filtering array without duplicates.</returns>
    public static IFiltering<TEntity>[] Merge<TEntity>(this IFiltering<TEntity>[] filtering,
        IFiltering<TEntity>[] source)
    {
        ArgumentNullException.ThrowIfNull(filtering);
        ArgumentNullException.ThrowIfNull(source);

        return filtering
            .Concat(source.Where(sourceItem => 
                filtering.All(filterItem => filterItem.ColumnName != sourceItem.ColumnName)))
            .ToArray();
    }

    private static List<IFiltering<TEntity>> Cast<TEntity>(
        IFiltering filteringItem,
        FilteringCastOptions<TEntity> options)
    {
        var list = new List<IFiltering<TEntity>>();

        // Use pattern matching for better performance and readability
        return filteringItem switch
        {
            CompositeFiltering compositeFiltering => CastCompositeFiltering(compositeFiltering, options),
            _ => CastSingleFiltering(filteringItem, options)
        };
    }

    private static List<IFiltering<TEntity>> CastCompositeFiltering<TEntity>(
        CompositeFiltering compositeFiltering,
        FilteringCastOptions<TEntity> options)
    {
        var values = compositeFiltering.Values.SelectMany(f => Cast(f, options)).ToArray();
        return new List<IFiltering<TEntity>> 
        { 
            new CompositeFiltering<TEntity>(compositeFiltering.CompositeOperator, values) 
        };
    }

    private static List<IFiltering<TEntity>> CastSingleFiltering<TEntity>(
        IFiltering filteringItem,
        FilteringCastOptions<TEntity> options)
    {
        var list = new List<IFiltering<TEntity>>();

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