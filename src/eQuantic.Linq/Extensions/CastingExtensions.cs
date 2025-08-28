using System.Runtime.CompilerServices;
using eQuantic.Linq.Casting;
using eQuantic.Linq.Filter;
using eQuantic.Linq.Sorter;

namespace eQuantic.Linq.Extensions;

/// <summary>
/// Extension methods for enhanced casting operations with modern patterns.
/// </summary>
public static class CastingExtensions
{
    /// <summary>
    /// Creates a fluent cast builder for the specified entity type.
    /// </summary>
    /// <typeparam name="TEntity">The target entity type.</typeparam>
    /// <param name="filters">The source filters (parameter for extension method syntax).</param>
    /// <returns>A fluent cast builder instance.</returns>
    public static FluentCastBuilder<TEntity> ToFluentBuilder<TEntity>(this IFiltering[] filters)
    {
        return FluentCastBuilder<TEntity>.Create();
    }

    /// <summary>
    /// Creates a fluent cast builder for the specified entity type.
    /// </summary>
    /// <typeparam name="TEntity">The target entity type.</typeparam>
    /// <param name="sorts">The source sorts (parameter for extension method syntax).</param>
    /// <returns>A fluent cast builder instance.</returns>
    public static FluentCastBuilder<TEntity> ToFluentBuilder<TEntity>(this ISorting[] sorts)
    {
        return FluentCastBuilder<TEntity>.Create();
    }

    /// <summary>
    /// Creates a property expression for the given property name using modern C# patterns.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="propertyName">The property name.</param>
    /// <returns>A lambda expression representing the property.</returns>
    public static Expression<Func<TEntity, object>> GetPropertyExpression<TEntity>(
        string propertyName, 
        [CallerArgumentExpression(nameof(propertyName))] string? propertyExpression = null)
    {
#if NET8_0_OR_GREATER
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
#else
        if (string.IsNullOrWhiteSpace(propertyName))
            throw new ArgumentException($"Property expression '{propertyExpression}' resolved to null or whitespace.", nameof(propertyName));
#endif
        
        var parameter = Expression.Parameter(typeof(TEntity), "x");
        var property = Expression.Property(parameter, propertyName);
        var converted = Expression.Convert(property, typeof(object));
        
        return Expression.Lambda<Func<TEntity, object>>(converted, parameter);
    }
}