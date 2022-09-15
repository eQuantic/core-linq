using System.Globalization;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using eQuantic.Linq.Exceptions;
using eQuantic.Linq.Extensions;

namespace eQuantic.Linq.Filter;

/// <summary>
/// Filtering
/// </summary>
/// <typeparam name="T"></typeparam>
/// <seealso cref="Filtering" />
public class Filtering<T> : Filtering, IFiltering<T>
{
    public Expression<Func<T, object>> Expression { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Filtering{T}"/> class.
    /// </summary>
    public Filtering()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Filtering{T}"/> class.
    /// </summary>
    /// <param name="expression">The expression.</param>
    /// <param name="stringValue">The string value.</param>
    /// <param name="operator">The operator.</param>
    /// <param name="useColumnFallback">Use column fallback</param>
    public Filtering(Expression<Func<T, object>> expression, string stringValue, FilterOperator? @operator = null,
        bool useColumnFallback = false)
        : base(expression.GetColumnName(useColumnFallback), stringValue, @operator)
    {
        this.Expression = expression;
    }

    /// <summary>
    /// Sets the column.
    /// </summary>
    /// <param name="expression">The expression.</param>
    /// <param name="useColumnFallback">Use column fallback</param>
    public void SetColumn(Expression<Func<T, object>> expression, bool useColumnFallback = false)
    {
        ColumnName = expression.GetColumnName(useColumnFallback);
    }
}

public class Filtering : IFiltering, IFormattable
{
    public const string DefaultFormat = "{0}:{1}({2})";
    public const string ArgsRegex = @"(?:[^,()]+((?:\((?>[^()]+|\((?<open>)|\)(?<-open>))*\)))*)+";
    protected const string FuncRegex = @"(\b[^()]+)\((.*)\)$";
    internal const string SimplifiedFormat = "propertyName:value";
    internal static readonly string ExpectedFormat = string.Format(DefaultFormat, "propertyName", "operator", "value");

    public Filtering()
    {
    }

    public Filtering(string columnName, string stringValue, FilterOperator? @operator = null)
    {
        this.ColumnName = columnName;

        if (@operator == null)
        {
            var parsedValue = ParseValue(stringValue);
            this.StringValue = parsedValue.StringValue;
            this.Operator = parsedValue.Operator;
        }
        else
        {
            this.StringValue = stringValue;
            this.Operator = @operator.Value;
        }
    }

    public string ColumnName { get; set; }
    public FilterOperator Operator { get; set; } = FilterOperator.Equal;
    public string StringValue { get; set; }

    public static IFiltering Parse(string query)
    {
        var columnNameAndValue = query.Split(':', 2, StringSplitOptions.RemoveEmptyEntries);

        if (columnNameAndValue.Length < 2)
        {
            throw new InvalidFormatException(nameof(Filtering), ExpectedFormat, SimplifiedFormat);
        }

        var columnName = columnNameAndValue[0];
        var value = columnNameAndValue[1];

        return new Filtering(columnName, value);
    }

    public static (FilterOperator Operator, string StringValue) ParseValue(string value)
    {
        var match = Regex.Match(value, FuncRegex);
        if (match.Success && match.Groups.Count == 3)
        {
            var @operator = match.Groups[1].Value;
            var operatorFilter = FilterOperatorValues.GetOperator(@operator);
            value = match.Groups[2].Value;

            return (operatorFilter, value);
        }

        return (FilterOperator.Equal, value);
    }

    public override string ToString()
    {
        return this.ToString(DefaultFormat, null);
    }

    public string ToString(string format, IFormatProvider formatProvider)
    {
        format ??= DefaultFormat;
        formatProvider ??= CultureInfo.InvariantCulture;
        var @operator = FilterOperatorValues.GetOperator(Operator);
        return string.Format(formatProvider, format, ColumnName, @operator, StringValue);
    }
}