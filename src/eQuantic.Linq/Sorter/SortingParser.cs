using eQuantic.Linq.Exceptions;

namespace eQuantic.Linq.Sorter;

/// <summary>
/// The sort parser class
/// </summary>
public static class SortingParser
{
    /// <summary>
    /// The default format
    /// </summary>
    private static readonly string ExpectedFormat =
        string.Format(Linq.Sorter.Sorting.DefaultFormat, "propertyName", "sortDirection");

    /// <summary>
    /// The descending
    /// </summary>
    private static readonly Dictionary<string, SortDirection> ValidDirections =
        new Dictionary<string, SortDirection>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "asc", SortDirection.Ascending },
            { "desc", SortDirection.Descending }
        };

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
    /// <exception cref="InvalidFormatException"></exception>
    /// <exception cref="InvalidFormatException"></exception>
    /// <returns>The sorts</returns>
    public static IEnumerable<ISorting> Parse(string values)
    {
        var sorts = new List<Linq.Sorter.Sorting>();
        var queryStringValues = values.Split(',');
        foreach (var queryStringValue in queryStringValues)
        {
            var columnNameAndDirection = queryStringValue.Split(':');

            var columnName = columnNameAndDirection[0];

            if (string.IsNullOrEmpty(columnName))
            {
                throw new InvalidFormatException(nameof(SortingParser), ExpectedFormat);
            }

            var direction = SortDirection.Ascending;

            if (columnNameAndDirection.Length > 1)
            {
                if (string.IsNullOrEmpty(columnNameAndDirection[1]))
                {
                    throw new InvalidFormatException(nameof(SortingParser), ExpectedFormat);
                }

                direction = ParseDirection(columnNameAndDirection[1].Trim());
            }

            sorts.Add(new Linq.Sorter.Sorting(columnName, direction));
        }

        return sorts;
    }

    /// <summary>
    /// Describes whether try parse
    /// </summary>
    /// <param name="values">The values</param>
    /// <param name="sortings">The sortings</param>
    /// <returns>The bool</returns>
    public static bool TryParse(string values, out IEnumerable<ISorting> sortings)
    {
        try
        {
            sortings = Parse(values);
            return true;
        }
        catch
        {
            sortings = null;
            return false;
        }
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

    /// <summary>
    /// Parses the direction using the specified direction name
    /// </summary>
    /// <param name="directionName">The direction name</param>
    /// <exception cref="FormatException">The sort direction '{directionName}' is invalid.</exception>
    /// <returns>The sort direction</returns>
    private static SortDirection ParseDirection(string directionName)
    {
        if (!ValidDirections.ContainsKey(directionName))
        {
            throw new FormatException($"The sort direction '{directionName}' is invalid.");
        }

        return directionName.Equals("desc", StringComparison.InvariantCultureIgnoreCase)
            ? SortDirection.Descending
            : SortDirection.Ascending;
    }
}