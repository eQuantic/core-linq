using System.Text.RegularExpressions;

namespace eQuantic.Linq.Filter;

/// <summary>
/// The filtering parser class
/// </summary>
public static class FilteringParser
{
    /// <summary>
    /// The list
    /// </summary>
    private static readonly Type[] ValidListTypes = [
        typeof(IEnumerable<>),
        typeof(ICollection<>),
        typeof(IList<>),
        typeof(List<>)
    ];

    /// <summary>
    /// Parses the values
    /// </summary>
    /// <param name="values">The values</param>
    /// <exception cref="ArgumentException"></exception>
    /// <returns>An enumerable of i filtering</returns>
    public static IEnumerable<IFiltering> Parse(string values)
    {
        if (string.IsNullOrEmpty(values)) 
            throw new ArgumentException(null, nameof(values));

        var matches = Regex.Matches(values, Filtering.ArgsRegex);
        return !matches.Any() ? 
            new List<IFiltering> { CompositeFiltering.ParseComposite(values) } : 
            matches.Select(m => CompositeFiltering.ParseComposite(m.Value.Trim()));
    }

    /// <summary>
    /// Describes whether is valid list type
    /// </summary>
    /// <param name="type">The type</param>
    /// <returns>The bool</returns>
    public static bool IsValidListType(Type type)
    {
        return type.IsGenericType && ValidListTypes.Any(t => t.IsAssignableFrom(type.GetGenericTypeDefinition()));
    }
}