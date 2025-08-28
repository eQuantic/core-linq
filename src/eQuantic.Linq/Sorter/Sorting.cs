using System.Globalization;
using System.Linq.Expressions;
using eQuantic.Linq.Exceptions;
using eQuantic.Linq.Extensions;

namespace eQuantic.Linq.Sorter;

public class Sorting<T> : Sorting, ISorting<T>
{
    public Expression<Func<T, object>> Expression { get; private set; }
    
    public Sorting(
        Expression<Func<T, object>> expression, 
        SortDirection sortDirection = SortDirection.Ascending,
        bool useColumnFallback = false)
        : base(GetColumnName(expression, useColumnFallback), sortDirection)
    {
        Expression = expression;
    }

    private static string GetColumnName(Expression<Func<T, object>> expression, bool useColumnFallback = false)
    {
        if (expression.Body is MemberExpression member) 
            return member.GetColumnName(useColumnFallback);
        
        var op = ((UnaryExpression)expression.Body).Operand;
        member = (MemberExpression)op;
        return member.GetColumnName(useColumnFallback);
    }
}

public class Sorting : ISorting
{
    public const string DefaultFormat = "{0}:{1}";
    internal static readonly string ExpectedFormat = string.Format(DefaultFormat, "propertyName", "direction");
    
    public Sorting(string columnName, SortDirection sortDirection)
    {
        this.ColumnName = columnName;
        this.SortDirection = sortDirection;
    }

    public string ColumnName { get; }
    public SortDirection SortDirection { get; }
    
    public static Sorting Parse(string query)
    {
        var columnNameAndValue = query.Split(':', 2, StringSplitOptions.RemoveEmptyEntries);

        if (columnNameAndValue.Length < 2)
        {
            throw new InvalidFormatException(nameof(Sorting), ExpectedFormat);
        }

        var columnName = columnNameAndValue[0];
        var direction = columnNameAndValue[1].ToLowerInvariant() switch
        {
            "desc" or "descending" => SortDirection.Descending,
            "asc" or "ascending" => SortDirection.Ascending,
            _ => SortDirection.Ascending // Default to ascending for any other value
        };

        return new Sorting(columnName, direction);
    }

    public static bool TryParse(string query, out Sorting sorting)
    {
        try
        {
            sorting = Parse(query);
            return true;
        }
        catch
        {
            sorting = null!;
            return false;
        }
    }
    
    public override string ToString()
    {
        return ToString(DefaultFormat, null);
    }

    /// <summary>
    /// Formats the sorting using modern switch expression for direction conversion.
    /// </summary>
    /// <param name="format">The format string to use.</param>
    /// <param name="formatProvider">The format provider.</param>
    /// <returns>Formatted string representation of the sorting.</returns>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        format ??= DefaultFormat;
        formatProvider ??= CultureInfo.InvariantCulture;
        
        var directionString = SortDirection switch
        {
            SortDirection.Ascending => "asc",
            SortDirection.Descending => "desc",
            _ => "asc" // Default fallback
        };
        
        return string.Format(formatProvider, format, ColumnName, directionString);
    }
}