using System.Collections.ObjectModel;

namespace eQuantic.Linq.Filter;

/// <summary>
/// Defines the available filter operators for individual property filtering conditions.
/// </summary>
public enum FilterOperator
{
    /// <summary>
    /// Tests for equality between the property value and the specified value.
    /// </summary>
    Equal = 0,
    
    /// <summary>
    /// Tests for inequality between the property value and the specified value.
    /// </summary>
    NotEqual = 1,
    
    /// <summary>
    /// Tests if the property value contains the specified substring (for string properties).
    /// </summary>
    Contains = 2,
    
    /// <summary>
    /// Tests if the property value starts with the specified substring (for string properties).
    /// </summary>
    StartsWith = 3,
    
    /// <summary>
    /// Tests if the property value ends with the specified substring (for string properties).
    /// </summary>
    EndsWith = 4,
    
    /// <summary>
    /// Tests if the property value is greater than the specified value (for comparable types).
    /// </summary>
    GreaterThan = 5,
    
    /// <summary>
    /// Tests if the property value is greater than or equal to the specified value (for comparable types).
    /// </summary>
    GreaterThanOrEqual = 6,
    
    /// <summary>
    /// Tests if the property value is less than the specified value (for comparable types).
    /// </summary>
    LessThan = 7,
    
    /// <summary>
    /// Tests if the property value is less than or equal to the specified value (for comparable types).
    /// </summary>
    LessThanOrEqual = 8,
    
    /// <summary>
    /// Tests if the property value does not contain the specified substring (for string properties).
    /// </summary>
    NotContains = 9
}

/// <summary>
/// Provides utility methods for working with composite operator values and their string representations.
/// </summary>
public static class CompositeOperatorValues
{
    /// <summary>
    /// Gets a read-only dictionary mapping composite operators to their string representations.
    /// </summary>
    public static ReadOnlyDictionary<CompositeOperator, string> Values => new ReadOnlyDictionary<CompositeOperator, string>(new Dictionary<CompositeOperator, string>
    {
        {CompositeOperator.And, "and"},
        {CompositeOperator.Or, "or"},
        {CompositeOperator.Any, "any"},
        {CompositeOperator.All, "all"}
    });

    /// <summary>
    /// Checks if composite operator string exists using pattern matching.
    /// </summary>
    /// <param name="compositeOperator">The composite operator string to check.</param>
    /// <returns>True if exists, false otherwise.</returns>
    public static bool Exists(string compositeOperator) => 
        compositeOperator?.ToLowerInvariant() switch
        {
            "and" or "or" or "any" or "all" => true,
            _ => false
        };

    /// <summary>
    /// Converts CompositeOperator enum to string representation using switch expression.
    /// </summary>
    /// <param name="compositeOperator">The composite operator enum.</param>
    /// <returns>String representation of the composite operator.</returns>
    public static string GetOperator(CompositeOperator compositeOperator) => compositeOperator switch
    {
        CompositeOperator.And => "and",
        CompositeOperator.Or => "or",
        CompositeOperator.Any => "any",
        CompositeOperator.All => "all",
        _ => throw new ArgumentOutOfRangeException(nameof(compositeOperator), compositeOperator, "Unknown composite operator")
    };

    /// <summary>
    /// Converts string representation to CompositeOperator enum using switch expression.
    /// </summary>
    /// <param name="compositeOperator">String representation of the composite operator.</param>
    /// <returns>CompositeOperator enum value or null if invalid.</returns>
    public static CompositeOperator? GetOperator(string compositeOperator) => 
        compositeOperator?.ToLowerInvariant() switch
        {
            "and" => CompositeOperator.And,
            "or" => CompositeOperator.Or,
            "any" => CompositeOperator.Any,
            "all" => CompositeOperator.All,
            _ => null
        };
}

/// <summary>
/// Provides utility methods for working with filter operator values and their string representations.
/// </summary>
public static class FilterOperatorValues
{
    /// <summary>
    /// Gets a read-only dictionary mapping filter operators to their string representations.
    /// </summary>
    public static ReadOnlyDictionary<FilterOperator, string> Values => new ReadOnlyDictionary<FilterOperator, string>(new Dictionary<FilterOperator, string>
    {
        {FilterOperator.Equal, "eq"},
        {FilterOperator.NotEqual, "neq"},
        {FilterOperator.Contains, "ct"},
        {FilterOperator.NotContains, "nct"},
        {FilterOperator.StartsWith, "sw"},
        {FilterOperator.EndsWith, "ew"},
        {FilterOperator.GreaterThan, "gt"},
        {FilterOperator.GreaterThanOrEqual, "gte"},
        {FilterOperator.LessThan, "lt"},
        {FilterOperator.LessThanOrEqual, "lte"}
    });

    /// <summary>
    /// Converts FilterOperator enum to string representation using switch expression.
    /// </summary>
    /// <param name="filterOperator">The filter operator enum.</param>
    /// <returns>String representation of the operator.</returns>
    public static string GetOperator(FilterOperator filterOperator) => filterOperator switch
    {
        FilterOperator.Equal => "eq",
        FilterOperator.NotEqual => "neq",
        FilterOperator.Contains => "ct",
        FilterOperator.NotContains => "nct",
        FilterOperator.StartsWith => "sw",
        FilterOperator.EndsWith => "ew",
        FilterOperator.GreaterThan => "gt",
        FilterOperator.GreaterThanOrEqual => "gte",
        FilterOperator.LessThan => "lt",
        FilterOperator.LessThanOrEqual => "lte",
        _ => throw new ArgumentOutOfRangeException(nameof(filterOperator), filterOperator, "Unknown filter operator")
    };

    /// <summary>
    /// Converts string representation to FilterOperator enum using switch expression.
    /// </summary>
    /// <param name="filterOperator">String representation of the operator.</param>
    /// <returns>FilterOperator enum value.</returns>
    /// <exception cref="FormatException">Thrown when the operator string is invalid.</exception>
    public static FilterOperator GetOperator(string filterOperator) => 
        filterOperator?.ToLowerInvariant() switch
        {
            "eq" => FilterOperator.Equal,
            "neq" => FilterOperator.NotEqual,
            "ct" => FilterOperator.Contains,
            "nct" => FilterOperator.NotContains,
            "sw" => FilterOperator.StartsWith,
            "ew" => FilterOperator.EndsWith,
            "gt" => FilterOperator.GreaterThan,
            "gte" => FilterOperator.GreaterThanOrEqual,
            "lt" => FilterOperator.LessThan,
            "lte" => FilterOperator.LessThanOrEqual,
            _ => throw new FormatException($"Operator '{filterOperator}' is invalid.")
        };
}