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

    public static bool Exists(string compositeOperator)
    {
        return Values.Any(kv => kv.Value.Equals(compositeOperator, StringComparison.InvariantCultureIgnoreCase));
    }

    public static string GetOperator(CompositeOperator compositeOperator)
    {
        return Values[compositeOperator];
    }

    public static CompositeOperator? GetOperator(string compositeOperator)
    {
        if (!Exists(compositeOperator)) return null;

        return Values.FirstOrDefault(kv => kv.Value.Equals(compositeOperator, StringComparison.InvariantCultureIgnoreCase)).Key;
    }
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

    public static string GetOperator(FilterOperator filterOperator)
    {
        return Values[filterOperator];
    }

    public static FilterOperator GetOperator(string filterOperator)
    {
        Func<KeyValuePair<FilterOperator, string>, bool> exp = kv =>
            kv.Value.Equals(filterOperator, StringComparison.InvariantCultureIgnoreCase);

        if (!Values.Any(exp))
        {
            throw new FormatException($"Operator '{filterOperator}' is invalid.");
        }
            
        return Values.FirstOrDefault(exp).Key;
    }
}