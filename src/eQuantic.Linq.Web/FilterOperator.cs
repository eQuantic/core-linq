namespace eQuantic.Linq.Web;

/// <summary>
/// Comparison operators of the query-string filter grammar, used by
/// <see cref="QueryFilterBuilder{T}"/> to build filters in code. Each value maps to a grammar
/// token (<c>eq</c>, <c>gt</c>, <c>ct</c>, …).
/// </summary>
public enum FilterOperator
{
    /// <summary>Equality — <c>eq</c>.</summary>
    Equal,

    /// <summary>Inequality — <c>neq</c>.</summary>
    NotEqual,

    /// <summary>Greater than — <c>gt</c>.</summary>
    GreaterThan,

    /// <summary>Greater than or equal — <c>gte</c>.</summary>
    GreaterThanOrEqual,

    /// <summary>Less than — <c>lt</c>.</summary>
    LessThan,

    /// <summary>Less than or equal — <c>lte</c>.</summary>
    LessThanOrEqual,

    /// <summary>String contains — <c>ct</c>.</summary>
    Contains,

    /// <summary>String does not contain — <c>nct</c>.</summary>
    NotContains,

    /// <summary>String starts with — <c>sw</c>.</summary>
    StartsWith,

    /// <summary>String ends with — <c>ew</c>.</summary>
    EndsWith,
}

internal static class FilterOperatorTokens
{
    public static string ToToken(this FilterOperator op) => op switch
    {
        FilterOperator.Equal => "eq",
        FilterOperator.NotEqual => "neq",
        FilterOperator.GreaterThan => "gt",
        FilterOperator.GreaterThanOrEqual => "gte",
        FilterOperator.LessThan => "lt",
        FilterOperator.LessThanOrEqual => "lte",
        FilterOperator.Contains => "ct",
        FilterOperator.NotContains => "nct",
        FilterOperator.StartsWith => "sw",
        FilterOperator.EndsWith => "ew",
        _ => throw new ArgumentOutOfRangeException(nameof(op), op, "Unknown filter operator."),
    };

    public static bool TryFromToken(string token, out FilterOperator op)
    {
        switch (token)
        {
            case "eq": op = FilterOperator.Equal; return true;
            case "neq": op = FilterOperator.NotEqual; return true;
            case "gt": op = FilterOperator.GreaterThan; return true;
            case "gte": op = FilterOperator.GreaterThanOrEqual; return true;
            case "lt": op = FilterOperator.LessThan; return true;
            case "lte": op = FilterOperator.LessThanOrEqual; return true;
            case "ct": op = FilterOperator.Contains; return true;
            case "nct": op = FilterOperator.NotContains; return true;
            case "sw": op = FilterOperator.StartsWith; return true;
            case "ew": op = FilterOperator.EndsWith; return true;
            default: op = default; return false;
        }
    }
}
