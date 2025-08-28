using System.Linq.Expressions;
using eQuantic.Linq.Sorter;

namespace eQuantic.Linq;

/// <summary>
/// Fluent API for building queries with support for value types, records, and classes.
/// This implementation works directly with LINQ to Objects and IQueryable without dependency on EntityFilter.
/// </summary>
/// <typeparam name="TEntity">The type of entity to query (value type, record, or class).</typeparam>
public sealed class ValueQuery<TEntity> : IValueQuery<TEntity>, IValueComparisonQuery<TEntity>
{
    private readonly List<Expression<Func<TEntity, bool>>> _filters = [];
    private readonly List<(LambdaExpression KeySelector, SortDirection Direction)> _sorts = [];

    /// <summary>
    /// Creates a new query instance for the specified entity type.
    /// </summary>
    /// <returns>A new query builder instance.</returns>
    public static ValueQuery<TEntity> Create() => new();

    /// <summary>
    /// Adds a where condition using AND logic.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>The current query instance for chaining.</returns>
    public ValueQuery<TEntity> Where(Expression<Func<TEntity, bool>> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));
            
        _filters.Add(predicate);
        return this;
    }

    /// <summary>
    /// Adds a condition using AND logic (alias for Where).
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>The current query instance for chaining.</returns>
    public ValueQuery<TEntity> And(Expression<Func<TEntity, bool>> predicate)
    {
        return Where(predicate);
    }

    /// <summary>
    /// Adds a condition using OR logic.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>The current query instance for chaining.</returns>
    public ValueQuery<TEntity> Or(Expression<Func<TEntity, bool>> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        // For OR logic, we combine with the last filter if exists, otherwise treat as AND
        if (_filters.Count > 0)
        {
            var lastFilter = _filters[^1];
            _filters[^1] = CombineWithOr(lastFilter, predicate);
        }
        else
        {
            _filters.Add(predicate);
        }

        return this;
    }

    /// <summary>
    /// Adds an ascending sort by the specified property.
    /// </summary>
    /// <typeparam name="TKey">The type of the sort key.</typeparam>
    /// <param name="keySelector">The property selector for sorting.</param>
    /// <returns>The current query instance for chaining.</returns>
    public ValueQuery<TEntity> OrderBy<TKey>(Expression<Func<TEntity, TKey>> keySelector)
    {
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));
            
        _sorts.Clear(); // OrderBy resets the sort chain
        _sorts.Add((keySelector, SortDirection.Ascending));
        return this;
    }

    /// <summary>
    /// Adds a descending sort by the specified property.
    /// </summary>
    /// <typeparam name="TKey">The type of the sort key.</typeparam>
    /// <param name="keySelector">The property selector for sorting.</param>
    /// <returns>The current query instance for chaining.</returns>
    public ValueQuery<TEntity> OrderByDescending<TKey>(Expression<Func<TEntity, TKey>> keySelector)
    {
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));
            
        _sorts.Clear(); // OrderByDescending resets the sort chain
        _sorts.Add((keySelector, SortDirection.Descending));
        return this;
    }

    /// <summary>
    /// Adds an additional ascending sort criterion.
    /// </summary>
    /// <typeparam name="TKey">The type of the sort key.</typeparam>
    /// <param name="keySelector">The property selector for sorting.</param>
    /// <returns>The current query instance for chaining.</returns>
    public ValueQuery<TEntity> ThenBy<TKey>(Expression<Func<TEntity, TKey>> keySelector)
    {
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));
            
        _sorts.Add((keySelector, SortDirection.Ascending));
        return this;
    }

    /// <summary>
    /// Adds an additional descending sort criterion.
    /// </summary>
    /// <typeparam name="TKey">The type of the sort key.</typeparam>
    /// <param name="keySelector">The property selector for sorting.</param>
    /// <returns>The current query instance for chaining.</returns>
    public ValueQuery<TEntity> ThenByDescending<TKey>(Expression<Func<TEntity, TKey>> keySelector)
    {
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));
            
        _sorts.Add((keySelector, SortDirection.Descending));
        return this;
    }

    /// <summary>
    /// Applies the query to the specified enumerable collection.
    /// </summary>
    /// <param name="source">The source enumerable collection.</param>
    /// <returns>The filtered and sorted enumerable collection.</returns>
    public IEnumerable<TEntity> ApplyTo(IEnumerable<TEntity> source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        var result = source;

        // Apply filters
        foreach (var filter in _filters)
        {
            result = result.Where(filter.Compile());
        }

        // Apply sorts
        if (_sorts.Count > 0)
        {
            var ordered = ApplySorting(result, _sorts[0]);
            
            for (int i = 1; i < _sorts.Count; i++)
            {
                ordered = ApplyThenBy(ordered, _sorts[i]);
            }
            
            return ordered;
        }

        return result;
    }

    /// <summary>
    /// Applies the query to the specified queryable collection.
    /// </summary>
    /// <param name="source">The source queryable collection.</param>
    /// <returns>The filtered and sorted queryable collection.</returns>
    public IQueryable<TEntity> ApplyTo(IQueryable<TEntity> source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        var result = source;

        // Apply filters
        foreach (var filter in _filters)
        {
            result = result.Where(filter);
        }

        // Apply sorts
        if (_sorts.Count > 0)
        {
            var ordered = ApplySortingQueryable(result, _sorts[0]);
            
            for (int i = 1; i < _sorts.Count; i++)
            {
                ordered = ApplyThenByQueryable(ordered, _sorts[i]);
            }
            
            return ordered;
        }

        return result;
    }

    /// <summary>
    /// Gets the filter expression if filters are present.
    /// </summary>
    /// <returns>The combined filter expression, or null if no filters.</returns>
    public Expression<Func<TEntity, bool>>? GetFilterExpression()
    {
        if (_filters.Count == 0) return null;
        if (_filters.Count == 1) return _filters[0];

        // Combine multiple filters with AND logic
        var combined = _filters[0];
        for (int i = 1; i < _filters.Count; i++)
        {
            combined = CombineWithAnd(combined, _filters[i]);
        }
        
        return combined;
    }

    #region IValueComparisonQuery Implementation

    /// <summary>
    /// Adds a greater than condition for comparable properties.
    /// </summary>
    public ValueQuery<TEntity> WhereGreaterThan<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, TProperty value)
        where TProperty : IComparable<TProperty>
    {
        if (propertySelector == null)
            throw new ArgumentNullException(nameof(propertySelector));
            
        var parameter = propertySelector.Parameters.First();
        var property = propertySelector.Body;
        var constant = Expression.Constant(value, typeof(TProperty));
        var comparison = Expression.GreaterThan(property, constant);
        
        return Where(Expression.Lambda<Func<TEntity, bool>>(comparison, parameter));
    }

    /// <summary>
    /// Adds a greater than or equal condition for comparable properties.
    /// </summary>
    public ValueQuery<TEntity> WhereGreaterThanOrEqual<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, TProperty value)
        where TProperty : IComparable<TProperty>
    {
        if (propertySelector == null)
            throw new ArgumentNullException(nameof(propertySelector));
            
        var parameter = propertySelector.Parameters.First();
        var property = propertySelector.Body;
        var constant = Expression.Constant(value, typeof(TProperty));
        var comparison = Expression.GreaterThanOrEqual(property, constant);
        
        return Where(Expression.Lambda<Func<TEntity, bool>>(comparison, parameter));
    }

    /// <summary>
    /// Adds a less than condition for comparable properties.
    /// </summary>
    public ValueQuery<TEntity> WhereLessThan<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, TProperty value)
        where TProperty : IComparable<TProperty>
    {
        if (propertySelector == null)
            throw new ArgumentNullException(nameof(propertySelector));
            
        var parameter = propertySelector.Parameters.First();
        var property = propertySelector.Body;
        var constant = Expression.Constant(value, typeof(TProperty));
        var comparison = Expression.LessThan(property, constant);
        
        return Where(Expression.Lambda<Func<TEntity, bool>>(comparison, parameter));
    }

    /// <summary>
    /// Adds a less than or equal condition for comparable properties.
    /// </summary>
    public ValueQuery<TEntity> WhereLessThanOrEqual<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, TProperty value)
        where TProperty : IComparable<TProperty>
    {
        if (propertySelector == null)
            throw new ArgumentNullException(nameof(propertySelector));
            
        var parameter = propertySelector.Parameters.First();
        var property = propertySelector.Body;
        var constant = Expression.Constant(value, typeof(TProperty));
        var comparison = Expression.LessThanOrEqual(property, constant);
        
        return Where(Expression.Lambda<Func<TEntity, bool>>(comparison, parameter));
    }

    /// <summary>
    /// Adds an equals condition for equatable properties.
    /// </summary>
    public ValueQuery<TEntity> WhereEquals<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, TProperty value)
        where TProperty : IEquatable<TProperty>
    {
        if (propertySelector == null)
            throw new ArgumentNullException(nameof(propertySelector));
            
        var parameter = propertySelector.Parameters.First();
        var property = propertySelector.Body;
        var constant = Expression.Constant(value, typeof(TProperty));
        var comparison = Expression.Equal(property, constant);
        
        return Where(Expression.Lambda<Func<TEntity, bool>>(comparison, parameter));
    }

    /// <summary>
    /// Adds a not equals condition for equatable properties.
    /// </summary>
    public ValueQuery<TEntity> WhereNotEquals<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, TProperty value)
        where TProperty : IEquatable<TProperty>
    {
        if (propertySelector == null)
            throw new ArgumentNullException(nameof(propertySelector));
            
        var parameter = propertySelector.Parameters.First();
        var property = propertySelector.Body;
        var constant = Expression.Constant(value, typeof(TProperty));
        var comparison = Expression.NotEqual(property, constant);
        
        return Where(Expression.Lambda<Func<TEntity, bool>>(comparison, parameter));
    }

    /// <summary>
    /// Adds a between condition for comparable properties.
    /// </summary>
    public ValueQuery<TEntity> WhereBetween<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, TProperty from, TProperty to)
        where TProperty : IComparable<TProperty>
    {
        if (propertySelector == null)
            throw new ArgumentNullException(nameof(propertySelector));
            
        var parameter = propertySelector.Parameters.First();
        var property = propertySelector.Body;
        var fromConstant = Expression.Constant(from, typeof(TProperty));
        var toConstant = Expression.Constant(to, typeof(TProperty));
        
        var greaterThanOrEqual = Expression.GreaterThanOrEqual(property, fromConstant);
        var lessThanOrEqual = Expression.LessThanOrEqual(property, toConstant);
        var betweenExpression = Expression.AndAlso(greaterThanOrEqual, lessThanOrEqual);
        
        return Where(Expression.Lambda<Func<TEntity, bool>>(betweenExpression, parameter));
    }

    /// <summary>
    /// Adds an "in" condition for equatable properties.
    /// </summary>
    public ValueQuery<TEntity> WhereIn<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, params TProperty[] values)
        where TProperty : IEquatable<TProperty>
    {
        return WhereIn(propertySelector, (IEnumerable<TProperty>)values);
    }

    /// <summary>
    /// Adds an "in" condition for equatable properties using a collection.
    /// </summary>
    public ValueQuery<TEntity> WhereIn<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, IEnumerable<TProperty> values)
        where TProperty : IEquatable<TProperty>
    {
        if (propertySelector == null)
            throw new ArgumentNullException(nameof(propertySelector));
        if (values == null)
            throw new ArgumentNullException(nameof(values));
            
        var valuesList = values.ToList();
        if (valuesList.Count == 0)
        {
            // Empty collection means no matches
            Expression<Func<TEntity, bool>> falsePredicate = _ => false;
            return Where(falsePredicate);
        }

        var parameter = propertySelector.Parameters.First();
        var property = propertySelector.Body;
        var valuesConstant = Expression.Constant(valuesList, typeof(List<TProperty>));
        var containsMethod = typeof(List<TProperty>).GetMethod("Contains", new[] { typeof(TProperty) })!;
        var containsCall = Expression.Call(valuesConstant, containsMethod, property);
        
        return Where(Expression.Lambda<Func<TEntity, bool>>(containsCall, parameter));
    }

    /// <summary>
    /// Adds a "not in" condition for equatable properties.
    /// </summary>
    public ValueQuery<TEntity> WhereNotIn<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, params TProperty[] values)
        where TProperty : IEquatable<TProperty>
    {
        return WhereNotIn(propertySelector, (IEnumerable<TProperty>)values);
    }

    /// <summary>
    /// Adds a "not in" condition for equatable properties using a collection.
    /// </summary>
    public ValueQuery<TEntity> WhereNotIn<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, IEnumerable<TProperty> values)
        where TProperty : IEquatable<TProperty>
    {
        if (propertySelector == null)
            throw new ArgumentNullException(nameof(propertySelector));
        if (values == null)
            throw new ArgumentNullException(nameof(values));
            
        var valuesList = values.ToList();
        if (valuesList.Count == 0)
        {
            // Empty collection means all match (not in empty set)
            Expression<Func<TEntity, bool>> truePredicate = _ => true;
            return Where(truePredicate);
        }

        var parameter = propertySelector.Parameters.First();
        var property = propertySelector.Body;
        var valuesConstant = Expression.Constant(valuesList, typeof(List<TProperty>));
        var containsMethod = typeof(List<TProperty>).GetMethod("Contains", new[] { typeof(TProperty) })!;
        var containsCall = Expression.Call(valuesConstant, containsMethod, property);
        var notContains = Expression.Not(containsCall);
        
        return Where(Expression.Lambda<Func<TEntity, bool>>(notContains, parameter));
    }

    #endregion

    #region Private Helper Methods

    private static Expression<Func<TEntity, bool>> CombineWithAnd(Expression<Func<TEntity, bool>> expr1, Expression<Func<TEntity, bool>> expr2)
    {
        var parameter = Expression.Parameter(typeof(TEntity), "x");
        var body = Expression.AndAlso(
            Expression.Invoke(expr1, parameter),
            Expression.Invoke(expr2, parameter)
        );
        return Expression.Lambda<Func<TEntity, bool>>(body, parameter);
    }

    private static Expression<Func<TEntity, bool>> CombineWithOr(Expression<Func<TEntity, bool>> expr1, Expression<Func<TEntity, bool>> expr2)
    {
        var parameter = Expression.Parameter(typeof(TEntity), "x");
        var body = Expression.OrElse(
            Expression.Invoke(expr1, parameter),
            Expression.Invoke(expr2, parameter)
        );
        return Expression.Lambda<Func<TEntity, bool>>(body, parameter);
    }

    private static IOrderedEnumerable<TEntity> ApplySorting(IEnumerable<TEntity> source, (LambdaExpression KeySelector, SortDirection Direction) sort)
    {
        var keySelector = sort.KeySelector.Compile();
        return sort.Direction == SortDirection.Ascending
            ? source.OrderBy(x => keySelector.DynamicInvoke(x))
            : source.OrderByDescending(x => keySelector.DynamicInvoke(x));
    }

    private static IOrderedEnumerable<TEntity> ApplyThenBy(IOrderedEnumerable<TEntity> source, (LambdaExpression KeySelector, SortDirection Direction) sort)
    {
        var keySelector = sort.KeySelector.Compile();
        return sort.Direction == SortDirection.Ascending
            ? source.ThenBy(x => keySelector.DynamicInvoke(x))
            : source.ThenByDescending(x => keySelector.DynamicInvoke(x));
    }

    /// <summary>
    /// Applies initial sorting to queryable using switch expression for method selection.
    /// </summary>
    private static IOrderedQueryable<TEntity> ApplySortingQueryable(IQueryable<TEntity> source, (LambdaExpression KeySelector, SortDirection Direction) sort)
    {
        var methodName = sort.Direction switch
        {
            SortDirection.Ascending => "OrderBy",
            SortDirection.Descending => "OrderByDescending",
            _ => "OrderBy" // Default fallback
        };
        
        var keyType = sort.KeySelector.ReturnType;
        
        return (IOrderedQueryable<TEntity>)source.Provider.CreateQuery<TEntity>(
            Expression.Call(
                typeof(Queryable),
                methodName,
                new[] { typeof(TEntity), keyType },
                source.Expression,
                Expression.Quote(sort.KeySelector)
            )
        );
    }

    /// <summary>
    /// Applies secondary sorting to queryable using switch expression for method selection.
    /// </summary>
    private static IOrderedQueryable<TEntity> ApplyThenByQueryable(IOrderedQueryable<TEntity> source, (LambdaExpression KeySelector, SortDirection Direction) sort)
    {
        var methodName = sort.Direction switch
        {
            SortDirection.Ascending => "ThenBy",
            SortDirection.Descending => "ThenByDescending",
            _ => "ThenBy" // Default fallback
        };
        
        var keyType = sort.KeySelector.ReturnType;
        
        return (IOrderedQueryable<TEntity>)source.Provider.CreateQuery<TEntity>(
            Expression.Call(
                typeof(Queryable),
                methodName,
                new[] { typeof(TEntity), keyType },
                source.Expression,
                Expression.Quote(sort.KeySelector)
            )
        );
    }

    #endregion
}

/// <summary>
/// Static factory methods for creating value queries.
/// </summary>
public static class ValueQuery
{
    /// <summary>
    /// Creates a new query for the specified entity type (supports value types, records, and classes).
    /// </summary>
    /// <typeparam name="TEntity">The entity type to query.</typeparam>
    /// <returns>A new value query builder instance.</returns>
    public static ValueQuery<TEntity> For<TEntity>() => ValueQuery<TEntity>.Create();
}