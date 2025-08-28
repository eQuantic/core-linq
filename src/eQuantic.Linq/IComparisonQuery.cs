using System.Linq.Expressions;
using eQuantic.Linq.Filter;
using eQuantic.Linq.Sorter;

namespace eQuantic.Linq;

/// <summary>
/// Enhanced query interface with generic constraints for better type safety.
/// Provides specialized methods for comparable types.
/// </summary>
/// <typeparam name="TEntity">The entity type that supports comparison operations.</typeparam>
public interface IComparisonQuery<TEntity> : IQuery<TEntity> 
    where TEntity : class
{
    /// <summary>
    /// Adds a greater than condition for comparable properties.
    /// </summary>
    /// <typeparam name="TProperty">The comparable property type.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The current query instance for chaining.</returns>
    Query<TEntity> WhereGreaterThan<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, TProperty value)
        where TProperty : IComparable<TProperty>;

    /// <summary>
    /// Adds a greater than or equal condition for comparable properties.
    /// </summary>
    /// <typeparam name="TProperty">The comparable property type.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The current query instance for chaining.</returns>
    Query<TEntity> WhereGreaterThanOrEqual<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, TProperty value)
        where TProperty : IComparable<TProperty>;

    /// <summary>
    /// Adds a less than condition for comparable properties.
    /// </summary>
    /// <typeparam name="TProperty">The comparable property type.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The current query instance for chaining.</returns>
    Query<TEntity> WhereLessThan<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, TProperty value)
        where TProperty : IComparable<TProperty>;

    /// <summary>
    /// Adds a less than or equal condition for comparable properties.
    /// </summary>
    /// <typeparam name="TProperty">The comparable property type.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The current query instance for chaining.</returns>
    Query<TEntity> WhereLessThanOrEqual<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, TProperty value)
        where TProperty : IComparable<TProperty>;

    /// <summary>
    /// Adds an equals condition for equatable properties.
    /// </summary>
    /// <typeparam name="TProperty">The equatable property type.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The current query instance for chaining.</returns>
    Query<TEntity> WhereEquals<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, TProperty value)
        where TProperty : IEquatable<TProperty>;

    /// <summary>
    /// Adds a not equals condition for equatable properties.
    /// </summary>
    /// <typeparam name="TProperty">The equatable property type.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>The current query instance for chaining.</returns>
    Query<TEntity> WhereNotEquals<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, TProperty value)
        where TProperty : IEquatable<TProperty>;

    /// <summary>
    /// Adds a between condition for comparable properties.
    /// </summary>
    /// <typeparam name="TProperty">The comparable property type.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <param name="from">The lower bound (inclusive).</param>
    /// <param name="to">The upper bound (inclusive).</param>
    /// <returns>The current query instance for chaining.</returns>
    Query<TEntity> WhereBetween<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, TProperty from, TProperty to)
        where TProperty : IComparable<TProperty>;

    /// <summary>
    /// Adds an "in" condition for equatable properties.
    /// </summary>
    /// <typeparam name="TProperty">The equatable property type.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <param name="values">The values to check against.</param>
    /// <returns>The current query instance for chaining.</returns>
    Query<TEntity> WhereIn<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, params TProperty[] values)
        where TProperty : IEquatable<TProperty>;

    /// <summary>
    /// Adds an "in" condition for equatable properties using a collection.
    /// </summary>
    /// <typeparam name="TProperty">The equatable property type.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <param name="values">The collection of values to check against.</param>
    /// <returns>The current query instance for chaining.</returns>
    Query<TEntity> WhereIn<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, IEnumerable<TProperty> values)
        where TProperty : IEquatable<TProperty>;

    /// <summary>
    /// Adds a "not in" condition for equatable properties.
    /// </summary>
    /// <typeparam name="TProperty">The equatable property type.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <param name="values">The values to check against.</param>
    /// <returns>The current query instance for chaining.</returns>
    Query<TEntity> WhereNotIn<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, params TProperty[] values)
        where TProperty : IEquatable<TProperty>;

    /// <summary>
    /// Adds a "not in" condition for equatable properties using a collection.
    /// </summary>
    /// <typeparam name="TProperty">The equatable property type.</typeparam>
    /// <param name="propertySelector">The property selector.</param>
    /// <param name="values">The collection of values to check against.</param>
    /// <returns>The current query instance for chaining.</returns>
    Query<TEntity> WhereNotIn<TProperty>(Expression<Func<TEntity, TProperty>> propertySelector, IEnumerable<TProperty> values)
        where TProperty : IEquatable<TProperty>;
}