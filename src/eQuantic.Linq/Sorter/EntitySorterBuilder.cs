using System.Linq.Expressions;
using eQuantic.Linq.Expressions;

namespace eQuantic.Linq.Sorter;

/// <summary>
/// Entity Sorter Builder
/// </summary>
/// <typeparam name="T"></typeparam>
internal class EntitySorterBuilder<T>
{
    private readonly LambdaExpression keySelector;
    private readonly Type keyType;

    public EntitySorterBuilder(
        string propertyName, 
        bool useColumnFallback = false, 
        bool useNullCheckForNestedProperties = false)
    {
        var properties = EntityBuilder.GetProperties<T>(propertyName, useColumnFallback);
        keyType = properties.Last().PropertyType;
        var builder = LambdaBuilderFactory.Current.Create(typeof(T), keyType);
        keySelector = builder.BuildLambda(properties.ToArray(), useNullCheckForNestedProperties);
    }

    public EntitySorterBuilder(ISorting sorting) 
        : this(sorting.ColumnName, sorting.SortDirection)
    {
    }

    public EntitySorterBuilder(ISorting<T> sorting)
    {
        keySelector = sorting.Expression;
        keyType = typeof(object);
        Direction = sorting.SortDirection;
    }

    public EntitySorterBuilder(
        string propertyName, SortDirection sortDirection, 
        bool useNullCheckForNestedProperties = false) 
        : this(propertyName, useNullCheckForNestedProperties: useNullCheckForNestedProperties)
    {
        Direction = sortDirection;
    }

    public SortDirection Direction { get; set; }

    public Sorting<T> BuildSorting()
    {
        var sortingType = typeof(Sorting<>).MakeGenericType(new[] { typeof(T) });
        return (Sorting<T>) Activator.CreateInstance(sortingType, keySelector, Direction);
    }
    
    public IEntitySorter<T> BuildOrderByEntitySorter()
    {
        var typeArgs = new[] { typeof(T), keyType };

        var sortType = typeof(OrderBySorter<,>).MakeGenericType(typeArgs);

        return (IEntitySorter<T>)Activator.CreateInstance(sortType, keySelector, Direction);
    }

    public IEntitySorter<T> BuildThenByEntitySorter(IEntitySorter<T> baseSorter)
    {
        var typeArgs = new[] { typeof(T), keyType };

        var sortType = typeof(ThenBySorter<,>).MakeGenericType(typeArgs);

        return (IEntitySorter<T>)Activator.CreateInstance(sortType, baseSorter, keySelector, Direction);
    }
}