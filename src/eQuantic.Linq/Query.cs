using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using eQuantic.Linq.Filter;
using eQuantic.Linq.Sorter;

namespace eQuantic.Linq;

/// <summary>
/// Fluent API for building queries with filtering and sorting capabilities.
/// </summary>
/// <typeparam name="TEntity">The type of entity to query.</typeparam>
public sealed class Query<TEntity> : IQuery<TEntity>, IComparisonQuery<TEntity>
    where TEntity : class
{
    private readonly List<FilterExpression<TEntity>> _filters = [];
    private readonly List<SortExpression<TEntity>> _sorts = [];
    private CompositeOperator _currentOperator = CompositeOperator.And;

    /// <summary>
    /// Creates a new query instance for the specified entity type.
    /// </summary>
    /// <returns>A new query builder instance.</returns>
    public static Query<TEntity> Create() => new();

    /// <summary>
    /// Adds a where condition using AND logic.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>The current query instance for chaining.</returns>
    public Query<TEntity> Where(Expression<Func<TEntity, bool>> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));
        _filters.Add(new FilterExpression<TEntity>(predicate, CompositeOperator.And));
        return this;
    }

    /// <summary>
    /// Adds a where condition using AND logic with IFiltering.
    /// </summary>
    /// <param name="filtering">The filtering specification.</param>
    /// <returns>The current query instance for chaining.</returns>
    public Query<TEntity> Where(IFiltering filtering)
    {
        if (filtering == null)
            throw new ArgumentNullException(nameof(filtering));
        return Where([filtering]);
    }

    /// <summary>
    /// Adds multiple where conditions using AND logic with IFiltering array.
    /// </summary>
    /// <param name="filterings">The filtering specifications.</param>
    /// <returns>The current query instance for chaining.</returns>
    public Query<TEntity> Where(params IFiltering[] filterings)
    {
        if (filterings == null)
            throw new ArgumentNullException(nameof(filterings));
        if (filterings.Length == 0) return this;

        var filter = EntityFilter<TEntity>.Where(filterings);
        var expression = filter.GetExpression();
        
        if (expression != null)
        {
            _filters.Add(new FilterExpression<TEntity>(expression, CompositeOperator.And));
        }

        return this;
    }

    /// <summary>
    /// Adds a condition using AND logic (alias for Where).
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>The current query instance for chaining.</returns>
    public Query<TEntity> And(Expression<Func<TEntity, bool>> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));
        _filters.Add(new FilterExpression<TEntity>(predicate, CompositeOperator.And));
        return this;
    }

    /// <summary>
    /// Adds a condition using OR logic.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>The current query instance for chaining.</returns>
    public Query<TEntity> Or(Expression<Func<TEntity, bool>> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));
        _filters.Add(new FilterExpression<TEntity>(predicate, CompositeOperator.Or));
        return this;
    }

    /// <summary>
    /// Adds an ascending sort by the specified property.
    /// </summary>
    /// <typeparam name="TKey">The type of the sort key.</typeparam>
    /// <param name="keySelector">The property selector for sorting.</param>
    /// <returns>The current query instance for chaining.</returns>
    public Query<TEntity> OrderBy<TKey>(Expression<Func<TEntity, TKey>> keySelector)
    {
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));
        _sorts.Clear(); // OrderBy resets the sort chain
        _sorts.Add(new SortExpression<TEntity>(keySelector, SortDirection.Ascending, isFirstSort: true));
        return this;
    }

    /// <summary>
    /// Adds an ascending sort by the specified property name.
    /// </summary>
    /// <param name="propertyName">The name of the property to sort by.</param>
    /// <returns>The current query instance for chaining.</returns>
    public Query<TEntity> OrderBy(string propertyName)
    {
#if NET8_0_OR_GREATER
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
#else
        if (string.IsNullOrWhiteSpace(propertyName))
            throw new ArgumentException("Property name cannot be null or empty.", nameof(propertyName));
#endif

        _sorts.Clear(); // OrderBy resets the sort chain
        _sorts.Add(new SortExpression<TEntity>(propertyName, SortDirection.Ascending, isFirstSort: true));
        return this;
    }

    /// <summary>
    /// Adds a descending sort by the specified property.
    /// </summary>
    /// <typeparam name="TKey">The type of the sort key.</typeparam>
    /// <param name="keySelector">The property selector for sorting.</param>
    /// <returns>The current query instance for chaining.</returns>
    public Query<TEntity> OrderByDescending<TKey>(Expression<Func<TEntity, TKey>> keySelector)
    {
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));
        _sorts.Clear(); // OrderByDescending resets the sort chain
        _sorts.Add(new SortExpression<TEntity>(keySelector, SortDirection.Descending, isFirstSort: true));
        return this;
    }

    /// <summary>
    /// Adds a descending sort by the specified property name.
    /// </summary>
    /// <param name="propertyName">The name of the property to sort by.</param>
    /// <returns>The current query instance for chaining.</returns>
    public Query<TEntity> OrderByDescending(string propertyName)
    {
#if NET8_0_OR_GREATER
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
#else
        if (string.IsNullOrWhiteSpace(propertyName))
            throw new ArgumentException("Property name cannot be null or empty.", nameof(propertyName));
#endif

        _sorts.Clear(); // OrderByDescending resets the sort chain
        _sorts.Add(new SortExpression<TEntity>(propertyName, SortDirection.Descending, isFirstSort: true));
        return this;
    }

    /// <summary>
    /// Adds an additional ascending sort criterion.
    /// </summary>
    /// <typeparam name="TKey">The type of the sort key.</typeparam>
    /// <param name="keySelector">The property selector for sorting.</param>
    /// <returns>The current query instance for chaining.</returns>
    public Query<TEntity> ThenBy<TKey>(Expression<Func<TEntity, TKey>> keySelector)
    {
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));
        _sorts.Add(new SortExpression<TEntity>(keySelector, SortDirection.Ascending, isFirstSort: false));
        return this;
    }

    /// <summary>
    /// Adds an additional ascending sort criterion by property name.
    /// </summary>
    /// <param name="propertyName">The name of the property to sort by.</param>
    /// <returns>The current query instance for chaining.</returns>
    public Query<TEntity> ThenBy(string propertyName)
    {
#if NET8_0_OR_GREATER
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
#else
        if (string.IsNullOrWhiteSpace(propertyName))
            throw new ArgumentException("Property name cannot be null or empty.", nameof(propertyName));
#endif

        _sorts.Add(new SortExpression<TEntity>(propertyName, SortDirection.Ascending, isFirstSort: false));
        return this;
    }

    /// <summary>
    /// Adds an additional descending sort criterion.
    /// </summary>
    /// <typeparam name="TKey">The type of the sort key.</typeparam>
    /// <param name="keySelector">The property selector for sorting.</param>
    /// <returns>The current query instance for chaining.</returns>
    public Query<TEntity> ThenByDescending<TKey>(Expression<Func<TEntity, TKey>> keySelector)
    {
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));
        _sorts.Add(new SortExpression<TEntity>(keySelector, SortDirection.Descending, isFirstSort: false));
        return this;
    }

    /// <summary>
    /// Adds an additional descending sort criterion by property name.
    /// </summary>
    /// <param name="propertyName">The name of the property to sort by.</param>
    /// <returns>The current query instance for chaining.</returns>
    public Query<TEntity> ThenByDescending(string propertyName)
    {
#if NET8_0_OR_GREATER
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
#else
        if (string.IsNullOrWhiteSpace(propertyName))
            throw new ArgumentException("Property name cannot be null or empty.", nameof(propertyName));
#endif

        _sorts.Add(new SortExpression<TEntity>(propertyName, SortDirection.Descending, isFirstSort: false));
        return this;
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
        if (_filters.Count > 0)
        {
            var entityFilter = BuildEntityFilter();
            result = entityFilter.Filter(result);
        }

        // Apply sorts
        if (_sorts.Count > 0)
        {
            var entitySorter = BuildEntitySorter();
            result = entitySorter.Sort(result);
        }

        return result;
    }

    /// <summary>
    /// Applies the query asynchronously to the specified queryable collection.
    /// </summary>
    /// <param name="source">The source queryable collection.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The filtered and sorted array of entities.</returns>
    public async Task<TEntity[]> ApplyToAsync(IQueryable<TEntity> source, CancellationToken cancellationToken = default)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        var result = source;

        // Apply filters
        if (_filters.Count > 0)
        {
            var asyncFilter = BuildAsyncEntityFilter();
            result = asyncFilter.Filter(result);
        }

        // Apply sorts
        if (_sorts.Count > 0)
        {
            var asyncSorter = BuildAsyncEntitySorter();
            result = asyncSorter.Sort(result);
        }

        return await Task.FromResult(result.ToArray()).ConfigureAwait(false);
    }

    /// <summary>
    /// Applies the query asynchronously with enumerable result.
    /// </summary>
    /// <param name="source">The source queryable collection.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An async enumerable of filtered and sorted entities.</returns>
    public async IAsyncEnumerable<TEntity> ApplyToAsyncEnumerable(IQueryable<TEntity> source, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        var result = source;

        // Apply filters
        if (_filters.Count > 0)
        {
            var asyncFilter = BuildAsyncEntityFilter();
            result = asyncFilter.Filter(result);
            
            await foreach (var entity in asyncFilter.FilterAsyncEnumerable(result, cancellationToken).ConfigureAwait(false))
            {
                yield return entity;
            }
        }
        else if (_sorts.Count > 0)
        {
            // Apply sorts
            var asyncSorter = BuildAsyncEntitySorter();
            
            await foreach (var entity in asyncSorter.SortAsyncEnumerable(result, cancellationToken).ConfigureAwait(false))
            {
                yield return entity;
            }
        }
        else
        {
            // No filters or sorts - just enumerate
            foreach (var entity in result)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return entity;
            }
        }
    }

    /// <summary>
    /// Gets the filter expression if filters are present.
    /// </summary>
    /// <returns>The combined filter expression, or null if no filters.</returns>
    public Expression<Func<TEntity, bool>>? GetFilterExpression()
    {
        if (_filters.Count == 0) return null;

        var entityFilter = BuildEntityFilter();
        return entityFilter.GetExpression();
    }

    /// <summary>
    /// Gets the sort expressions.
    /// </summary>
    /// <returns>Array of sorting specifications.</returns>
    public ISorting[] GetSortExpressions()
    {
        return _sorts.Select(s => s.ToSorting()).ToArray();
    }

    #region IComparisonQuery Implementation

    /// <summary>
    /// Adds a greater than condition for comparable properties.
    /// </summary>
    /// <typeparam name="TProperty">The comparable property type.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The current query instance for chaining.</returns>
    public Query<TEntity> WhereGreaterThan<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, TProperty value)
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
    /// <typeparam name="TProperty">The comparable property type.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The current query instance for chaining.</returns>
    public Query<TEntity> WhereGreaterThanOrEqual<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, TProperty value)
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
    /// <typeparam name="TProperty">The comparable property type.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The current query instance for chaining.</returns>
    public Query<TEntity> WhereLessThan<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, TProperty value)
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
    /// <typeparam name="TProperty">The comparable property type.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The current query instance for chaining.</returns>
    public Query<TEntity> WhereLessThanOrEqual<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, TProperty value)
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
    /// <typeparam name="TProperty">The equatable property type.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The current query instance for chaining.</returns>
    public Query<TEntity> WhereEquals<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, TProperty value)
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
    /// <typeparam name="TProperty">The equatable property type.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The current query instance for chaining.</returns>
    public Query<TEntity> WhereNotEquals<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, TProperty value)
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
    /// <typeparam name="TProperty">The comparable property type.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <param name="from">The lower bound (inclusive).</param>
    /// <param name="to">The upper bound (inclusive).</param>
    /// <returns>The current query instance for chaining.</returns>
    public Query<TEntity> WhereBetween<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, TProperty from, TProperty to)
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
    /// <typeparam name="TProperty">The equatable property type.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <param name="values">The values to check against.</param>
    /// <returns>The current query instance for chaining.</returns>
    public Query<TEntity> WhereIn<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, params TProperty[] values)
        where TProperty : IEquatable<TProperty>
    {
        return WhereIn(propertySelector, (IEnumerable<TProperty>)values);
    }

    /// <summary>
    /// Adds an "in" condition for equatable properties using a collection.
    /// </summary>
    /// <typeparam name="TProperty">The equatable property type.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <param name="values">The collection of values to check against.</param>
    /// <returns>The current query instance for chaining.</returns>
    public Query<TEntity> WhereIn<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, IEnumerable<TProperty> values)
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
    /// <typeparam name="TProperty">The equatable property type.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <param name="values">The values to check against.</param>
    /// <returns>The current query instance for chaining.</returns>
    public Query<TEntity> WhereNotIn<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, params TProperty[] values)
        where TProperty : IEquatable<TProperty>
    {
        return WhereNotIn(propertySelector, (IEnumerable<TProperty>)values);
    }

    /// <summary>
    /// Adds a "not in" condition for equatable properties using a collection.
    /// </summary>
    /// <typeparam name="TProperty">The equatable property type.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <param name="values">The collection of values to check against.</param>
    /// <returns>The current query instance for chaining.</returns>
    public Query<TEntity> WhereNotIn<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, IEnumerable<TProperty> values)
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

    private IEntityFilter<TEntity> BuildEntityFilter()
    {
        if (_filters.Count == 0)
            return EntityFilter<TEntity>.AsQueryable();

        if (_filters.Count == 1)
            return new WhereEntityFilter<TEntity>(_filters[0].Expression);

        // Combine multiple filters
        IEntityFilter<TEntity>? result = null;

        foreach (var filter in _filters)
        {
            if (result == null)
            {
                result = new WhereEntityFilter<TEntity>(filter.Expression);
            }
            else
            {
                result = new WhereEntityFilter<TEntity>(result, filter.Expression, filter.Operator);
            }
        }

        return result!;
    }

    private IAsyncEntityFilter<TEntity> BuildAsyncEntityFilter()
    {
        if (_filters.Count == 0)
            return AsyncEntityFilter<TEntity>.AsQueryable();

        if (_filters.Count == 1)
            return AsyncEntityFilter<TEntity>.Where(_filters[0].Expression);

        // For multiple filters, build expression and create async filter
        var entityFilter = BuildEntityFilter();
        var expression = entityFilter.GetExpression();
        
        return expression == null 
            ? AsyncEntityFilter<TEntity>.AsQueryable()
            : AsyncEntityFilter<TEntity>.Where(expression);
    }

    private IEntitySorter<TEntity> BuildEntitySorter()
    {
        if (_sorts.Count == 0)
            return EntitySorter<TEntity>.AsQueryable();

        var sortings = _sorts.Select(s => s.ToSorting()).ToArray();
        return EntitySorter<TEntity>.OrderBy(false, sortings);
    }

    private IAsyncEntitySorter<TEntity> BuildAsyncEntitySorter()
    {
        var sortings = _sorts.Select(s => s.ToSorting()).ToArray();
        return AsyncEntitySorter<TEntity>.OrderBy(false, sortings);
    }
}

/// <summary>
/// Static factory methods for creating queries.
/// </summary>
public static class Query
{
    /// <summary>
    /// Creates a new query for the specified entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to query.</typeparam>
    /// <returns>A new query builder instance.</returns>
    public static Query<TEntity> For<TEntity>() where TEntity : class => Query<TEntity>.Create();
}

/// <summary>
/// Represents a filter expression with its composite operator.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
internal sealed class FilterExpression<TEntity>
{
    public Expression<Func<TEntity, bool>> Expression { get; }
    public CompositeOperator Operator { get; }
    
    public FilterExpression(Expression<Func<TEntity, bool>> expression, CompositeOperator @operator)
    {
        Expression = expression;
        Operator = @operator;
    }
}

/// <summary>
/// Represents a sort expression with direction and position information.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
internal sealed class SortExpression<TEntity>
{
    public LambdaExpression? KeySelector { get; }
    public string? PropertyName { get; }
    public SortDirection Direction { get; }
    public bool IsFirstSort { get; }

    public SortExpression(LambdaExpression keySelector, SortDirection direction, bool isFirstSort)
    {
        KeySelector = keySelector;
        Direction = direction;
        IsFirstSort = isFirstSort;
    }

    public SortExpression(string propertyName, SortDirection direction, bool isFirstSort)
    {
        PropertyName = propertyName;
        Direction = direction;
        IsFirstSort = isFirstSort;
    }

    public ISorting ToSorting()
    {
        var columnName = PropertyName ?? ExtractPropertyName(KeySelector!);
        return new Sorting(columnName, Direction);
    }

    private static string ExtractPropertyName(LambdaExpression expression)
    {
        return expression.Body switch
        {
            MemberExpression member => member.Member.Name,
            UnaryExpression unary when unary.Operand is MemberExpression memberExpr => memberExpr.Member.Name,
            _ => throw new ArgumentException($"Unable to extract property name from expression: {expression}")
        };
    }
}