using System.Linq.Expressions;
using eQuantic.Linq.Filter;
using eQuantic.Linq.Filter.Casting;
using eQuantic.Linq.Sorter.Casting;

namespace eQuantic.Linq.Casting;

/// <summary>
/// Provides a fluent API for building type casting configurations.
/// </summary>
/// <typeparam name="TEntity">The target entity type.</typeparam>
public class FluentCastBuilder<TEntity>
{
    private readonly FilteringCastOptions<TEntity> _filteringOptions = new();
    private readonly SortingCastOptions<TEntity> _sortingOptions = new();

    /// <summary>
    /// Creates a new fluent cast builder for the specified entity type.
    /// </summary>
    /// <returns>A new fluent cast builder instance.</returns>
    public static FluentCastBuilder<TEntity> Create() => new();

    /// <summary>
    /// Maps a source column to a destination property with optional value transformation.
    /// </summary>
    /// <param name="sourceColumn">The source column name.</param>
    /// <param name="destinationProperty">The destination property expression.</param>
    /// <param name="valueTransform">Optional value transformation function.</param>
    /// <param name="operator">Optional filter operator override.</param>
    /// <returns>The current builder instance for chaining.</returns>
    public FluentCastBuilder<TEntity> MapFilter(
        string sourceColumn,
        Expression<Func<TEntity, object>> destinationProperty,
        Func<string, string>? valueTransform = null,
        FilterOperator? @operator = null)
    {
        SetNewStringValue? setValue = valueTransform != null 
            ? new SetNewStringValue(valueTransform) 
            : null;
            
        _filteringOptions.Map(sourceColumn, destinationProperty, setValue, @operator);
        return this;
    }

    /// <summary>
    /// Maps a source column to a destination property for sorting.
    /// </summary>
    /// <param name="sourceColumn">The source column name.</param>
    /// <param name="destinationProperty">The destination property expression.</param>
    /// <returns>The current builder instance for chaining.</returns>
    public FluentCastBuilder<TEntity> MapSorting(
        string sourceColumn,
        Expression<Func<TEntity, object>> destinationProperty)
    {
        _sortingOptions.Map(sourceColumn, destinationProperty);
        return this;
    }

    /// <summary>
    /// Creates a custom filter mapping with complete control over the transformation.
    /// </summary>
    /// <param name="sourceColumn">The source column name.</param>
    /// <param name="customMapping">Custom mapping function.</param>
    /// <returns>The current builder instance for chaining.</returns>
    public FluentCastBuilder<TEntity> CustomFilterMap(
        string sourceColumn,
        SetNewFiltering<TEntity> customMapping)
    {
        _filteringOptions.CustomMap(sourceColumn, customMapping);
        return this;
    }

    /// <summary>
    /// Creates a custom sorting mapping with complete control over the transformation.
    /// </summary>
    /// <param name="sourceColumn">The source column name.</param>
    /// <param name="customMapping">Custom mapping function.</param>
    /// <returns>The current builder instance for chaining.</returns>
    public FluentCastBuilder<TEntity> CustomSortingMap(
        string sourceColumn,
        SetNewSorting<TEntity> customMapping)
    {
        _sortingOptions.CustomMap(sourceColumn, customMapping);
        return this;
    }

    /// <summary>
    /// Excludes specific source columns from casting.
    /// </summary>
    /// <param name="sourceColumns">The source column names to exclude.</param>
    /// <returns>The current builder instance for chaining.</returns>
    public FluentCastBuilder<TEntity> Exclude(params string[] sourceColumns)
    {
        foreach (var column in sourceColumns)
        {
            _filteringOptions.Exclude(column);
            _sortingOptions.Exclude(column);
        }
        return this;
    }

    /// <summary>
    /// Configures the builder to exclude unmapped columns instead of throwing exceptions.
    /// </summary>
    /// <returns>The current builder instance for chaining.</returns>
    public FluentCastBuilder<TEntity> ExcludeUnmapped()
    {
        _filteringOptions.ExcludeUnmapped();
        _sortingOptions.ExcludeUnmapped();
        return this;
    }

    /// <summary>
    /// Configures the builder to throw exceptions for unmapped columns.
    /// </summary>
    /// <returns>The current builder instance for chaining.</returns>
    public FluentCastBuilder<TEntity> ThrowOnUnmapped()
    {
        _filteringOptions.ThrowUnmapped();
        _sortingOptions.ThrowUnmapped();
        return this;
    }

    /// <summary>
    /// Enables column fallback using attribute-based mapping.
    /// </summary>
    /// <param name="applicability">The fallback applicability mode.</param>
    /// <returns>The current builder instance for chaining.</returns>
    public FluentCastBuilder<TEntity> UseColumnFallback(
        ColumnFallbackApplicability applicability = ColumnFallbackApplicability.ToDestination)
    {
        _filteringOptions.UseColumnFallback(applicability);
        _sortingOptions.UseColumnFallback(applicability);
        return this;
    }

    /// <summary>
    /// Builds the filtering cast options and returns them for use with existing Cast methods.
    /// </summary>
    /// <returns>The configured filtering cast options.</returns>
    public FilteringCastOptions<TEntity> BuildFilteringOptions()
    {
        return _filteringOptions;
    }

    /// <summary>
    /// Builds the sorting cast options and returns them for use with existing Cast methods.
    /// </summary>
    /// <returns>The configured sorting cast options.</returns>
    public SortingCastOptions<TEntity> BuildSortingOptions()
    {
        return _sortingOptions;
    }

}