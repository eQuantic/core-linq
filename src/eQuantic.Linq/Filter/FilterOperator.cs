using System.Collections.ObjectModel;

namespace eQuantic.Linq.Filter;

public enum FilterOperator
{
    Equal = 0,
    NotEqual = 1,
    Contains = 2,
    StartsWith = 3,
    EndsWith = 4,
    GreaterThan = 5,
    GreaterThanOrEqual = 6,
    LessThan = 7,
    LessThanOrEqual = 8,
    NotContains = 9
}

public static class CompositeOperatorValues
{
    public static ReadOnlyDictionary<CompositeOperator, string> Values => new ReadOnlyDictionary<CompositeOperator, string>(new Dictionary<CompositeOperator, string>
    {
        {CompositeOperator.And, "and"},
        {CompositeOperator.Or, "or"}
    });

    /// <summary>
    /// Checks if composite operator string exists using pattern matching.
    /// </summary>
    /// <param name="compositeOperator">The composite operator string to check.</param>
    /// <returns>True if exists, false otherwise.</returns>
    public static bool Exists(string compositeOperator) => 
        compositeOperator?.ToLowerInvariant() switch
        {
            "and" or "or" => true,
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
            _ => null
        };
}

public static class FilterOperatorValues
{
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