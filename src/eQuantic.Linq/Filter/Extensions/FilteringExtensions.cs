using eQuantic.Linq.Filter.Casting;

namespace eQuantic.Linq.Filter.Extensions;

public static class FilteringExtensions
{
    public static Filtering<TEntity>[] Cast<TEntity>(this IFiltering[] filtering,
        Action<FilteringCastOptions<TEntity>> options)
    {
        var list = new List<Filtering<TEntity>>();
        var castOptions = new FilteringCastOptions<TEntity>();
        options.Invoke(castOptions);
            
        ThrowUnmapped(filtering, castOptions);
            
        foreach (var filteringItem in filtering)
        {
            list.AddRange(Cast(filteringItem, castOptions));
        }

        return list.ToArray();
    }
        
    public static Filtering<TEntity>[] Cast<TEntity>(this IFiltering filtering,
        Action<FilteringCastOptions<TEntity>> options)
    {
        var castOptions = new FilteringCastOptions<TEntity>();
        options.Invoke(castOptions);
            
        ThrowUnmapped(new []{ filtering }, castOptions);
            
        return Cast(filtering, castOptions).ToArray();
    }

    public static Filtering<TEntity>[] Merge<TEntity>(this Filtering<TEntity>[] filtering,
        Filtering<TEntity>[] source)
    {
        var list = filtering.ToList();
        list.AddRange(filtering.Where(item => source.All(s => s.ColumnName != item.ColumnName)));
        return list.ToArray();
    }

    private static List<Filtering<TEntity>> Cast<TEntity>(
        IFiltering filteringItem,
        FilteringCastOptions<TEntity> options)
    {
        var list = new List<Filtering<TEntity>>();
        var mapping = options.GetMapping();
        var excludeUnmapped = options.GetExcludeUnmapped();
            
        if (!excludeUnmapped && !mapping.Any(m =>
                m.Key.Equals(filteringItem.ColumnName, StringComparison.InvariantCultureIgnoreCase)))
        {
            list.Add(new Filtering<TEntity>
            {
                ColumnName = filteringItem.ColumnName,
                StringValue = filteringItem.StringValue,
                Operator = filteringItem.Operator
            });
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
            list.Add(new Filtering<TEntity>(
                map.Column,
                map.SetValue?.Invoke(filteringItem.StringValue) ?? filteringItem.StringValue,
                map.Operator ?? filteringItem.Operator));
        }
            
        return list;
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
            throw new InvalidCastException($"The following columns are unknown: {string.Join(", ", unmappedColumnNames)}");
        }
    }
}