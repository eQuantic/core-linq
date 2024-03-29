﻿using System.Globalization;
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
        var value = columnNameAndValue[1].Equals("desc", StringComparison.InvariantCultureIgnoreCase) ? 
            SortDirection.Descending : 
            SortDirection.Ascending;

        return new Sorting(columnName, value);
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

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        format ??= DefaultFormat;
        formatProvider ??= CultureInfo.InvariantCulture;
        var directionString = SortDirection == SortDirection.Ascending ? "asc" : "desc";
        return string.Format(formatProvider, format, ColumnName, directionString);
    }
}