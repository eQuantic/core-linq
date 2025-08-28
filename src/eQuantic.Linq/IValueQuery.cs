using System.Linq.Expressions;
using eQuantic.Linq.Filter;
using eQuantic.Linq.Sorter;

namespace eQuantic.Linq;

/// <summary>
/// Enhanced query interface for value types and records with better type safety.
/// </summary>
/// <typeparam name="TEntity">The entity type (value type, record, or class).</typeparam>
public interface IValueQuery<TEntity>
{
    /// <summary>
    /// Adds a where condition using AND logic.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>The current query instance for chaining.</returns>
    ValueQuery<TEntity> Where(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// Adds a condition using AND logic (alias for Where).
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>The current query instance for chaining.</returns>
    ValueQuery<TEntity> And(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// Adds a condition using OR logic.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>The current query instance for chaining.</returns>
    ValueQuery<TEntity> Or(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// Adds an ascending sort by the specified property.
    /// </summary>
    /// <typeparam name="TKey">The type of the sort key.</typeparam>
    /// <param name="keySelector">The property selector for sorting.</param>
    /// <returns>The current query instance for chaining.</returns>
    ValueQuery<TEntity> OrderBy<TKey>(Expression<Func<TEntity, TKey>> keySelector);

    /// <summary>
    /// Adds a descending sort by the specified property.
    /// </summary>
    /// <typeparam name="TKey">The type of the sort key.</typeparam>
    /// <param name="keySelector">The property selector for sorting.</param>
    /// <returns>The current query instance for chaining.</returns>
    ValueQuery<TEntity> OrderByDescending<TKey>(Expression<Func<TEntity, TKey>> keySelector);

    /// <summary>
    /// Adds an additional ascending sort criterion.
    /// </summary>
    /// <typeparam name="TKey">The type of the sort key.</typeparam>
    /// <param name="keySelector">The property selector for sorting.</param>
    /// <returns>The current query instance for chaining.</returns>
    ValueQuery<TEntity> ThenBy<TKey>(Expression<Func<TEntity, TKey>> keySelector);

    /// <summary>
    /// Adds an additional descending sort criterion.
    /// </summary>
    /// <typeparam name="TKey">The type of the sort key.</typeparam>
    /// <param name="keySelector">The property selector for sorting.</param>
    /// <returns>The current query instance for chaining.</returns>
    ValueQuery<TEntity> ThenByDescending<TKey>(Expression<Func<TEntity, TKey>> keySelector);

    /// <summary>
    /// Applies the query to the specified enumerable collection.
    /// </summary>
    /// <param name="source">The source enumerable collection.</param>
    /// <returns>The filtered and sorted enumerable collection.</returns>
    IEnumerable<TEntity> ApplyTo(IEnumerable<TEntity> source);

    /// <summary>
    /// Applies the query to the specified queryable collection.
    /// </summary>
    /// <param name="source">The source queryable collection.</param>
    /// <returns>The filtered and sorted queryable collection.</returns>
    IQueryable<TEntity> ApplyTo(IQueryable<TEntity> source);

    /// <summary>
    /// Gets the filter expression if filters are present.
    /// </summary>
    /// <returns>The combined filter expression, or null if no filters.</returns>
    Expression<Func<TEntity, bool>>? GetFilterExpression();
}

/// <summary>
/// Enhanced query interface for value types with comparison operations.
/// </summary>
/// <typeparam name="TEntity">The value entity type.</typeparam>
public interface IValueComparisonQuery<TEntity> : IValueQuery<TEntity>
{
    /// <summary>
    /// Adds a greater than condition for comparable properties.
    /// </summary>
    /// <typeparam name="TProperty">The comparable property type.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The current query instance for chaining.</returns>
    ValueQuery<TEntity> WhereGreaterThan<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, TProperty value)
        where TProperty : IComparable<TProperty>;

    /// <summary>
    /// Adds a greater than or equal condition for comparable properties.
    /// </summary>
    /// <typeparam name="TProperty">The comparable property type.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The current query instance for chaining.</returns>
    ValueQuery<TEntity> WhereGreaterThanOrEqual<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, TProperty value)
        where TProperty : IComparable<TProperty>;

    /// <summary>
    /// Adds a less than condition for comparable properties.
    /// </summary>
    /// <typeparam name="TProperty">The comparable property type.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The current query instance for chaining.</returns>
    ValueQuery<TEntity> WhereLessThan<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, TProperty value)
        where TProperty : IComparable<TProperty>;

    /// <summary>
    /// Adds a less than or equal condition for comparable properties.
    /// </summary>
    /// <typeparam name="TProperty">The comparable property type.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The current query instance for chaining.</returns>
    ValueQuery<TEntity> WhereLessThanOrEqual<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, TProperty value)
        where TProperty : IComparable<TProperty>;

    /// <summary>
    /// Adds an equals condition for equatable properties.
    /// </summary>
    /// <typeparam name="TProperty">The equatable property type.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The current query instance for chaining.</returns>
    ValueQuery<TEntity> WhereEquals<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, TProperty value)
        where TProperty : IEquatable<TProperty>;

    /// <summary>
    /// Adds a not equals condition for equatable properties.
    /// </summary>
    /// <typeparam name="TProperty">The equatable property type.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The current query instance for chaining.</returns>
    ValueQuery<TEntity> WhereNotEquals<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, TProperty value)
        where TProperty : IEquatable<TProperty>;

    /// <summary>
    /// Adds a between condition for comparable properties.
    /// </summary>
    /// <typeparam name="TProperty">The comparable property type.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <param name="from">The lower bound (inclusive).</param>
    /// <param name="to">The upper bound (inclusive).</param>
    /// <returns>The current query instance for chaining.</returns>
    ValueQuery<TEntity> WhereBetween<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, TProperty from, TProperty to)
        where TProperty : IComparable<TProperty>;

    /// <summary>
    /// Adds an "in" condition for equatable properties.
    /// </summary>
    /// <typeparam name="TProperty">The equatable property type.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <param name="values">The values to check against.</param>
    /// <returns>The current query instance for chaining.</returns>
    ValueQuery<TEntity> WhereIn<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, params TProperty[] values)
        where TProperty : IEquatable<TProperty>;

    /// <summary>
    /// Adds an "in" condition for equatable properties using a collection.
    /// </summary>
    /// <typeparam name="TProperty">The equatable property type.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <param name="values">The collection of values to check against.</param>
    /// <returns>The current query instance for chaining.</returns>
    ValueQuery<TEntity> WhereIn<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, IEnumerable<TProperty> values)
        where TProperty : IEquatable<TProperty>;

    /// <summary>
    /// Adds a "not in" condition for equatable properties.
    /// </summary>
    /// <typeparam name="TProperty">The equatable property type.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <param name="values">The values to check against.</param>
    /// <returns>The current query instance for chaining.</returns>
    ValueQuery<TEntity> WhereNotIn<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, params TProperty[] values)
        where TProperty : IEquatable<TProperty>;

    /// <summary>
    /// Adds a "not in" condition for equatable properties using a collection.
    /// </summary>
    /// <typeparam name="TProperty">The equatable property type.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <param name="values">The collection of values to check against.</param>
    /// <returns>The current query instance for chaining.</returns>
    ValueQuery<TEntity> WhereNotIn<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, IEnumerable<TProperty> values)
        where TProperty : IEquatable<TProperty>;
}