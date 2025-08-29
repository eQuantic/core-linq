using System.Linq.Expressions;
using System.Text.RegularExpressions;
using eQuantic.Linq.Extensions;

namespace eQuantic.Linq.Filter;

/// <summary>
/// Defines the composite operators available for combining multiple filtering conditions.
/// </summary>
public enum CompositeOperator
{
    /// <summary>
    /// Combines conditions using logical AND operation. All conditions must be true.
    /// </summary>
    And,
    
    /// <summary>
    /// Combines conditions using logical OR operation. At least one condition must be true.
    /// </summary>
    Or,
    
    /// <summary>
    /// Applies to collection properties. Returns true if any item in the collection matches the specified conditions.
    /// </summary>
    Any,
    
    /// <summary>
    /// Applies to collection properties. Returns true if all items in the collection match the specified conditions.
    /// </summary>
    All
}

/// <summary>
/// Represents a composite filtering operation that combines multiple filtering conditions using logical operators.
/// Supports And/Or operations for regular conditions and Any/All operations for collection-based filtering.
/// </summary>
public class CompositeFiltering : Filtering
{
    /// <summary>
    /// Initializes a new instance of the CompositeFiltering class with the specified operator and string filter expressions.
    /// </summary>
    /// <param name="compositeOperator">The composite operator to use for combining the conditions.</param>
    /// <param name="values">Array of string filter expressions to be parsed and combined.</param>
    public CompositeFiltering(CompositeOperator compositeOperator, params string[] values)
    {
        CompositeOperator = compositeOperator;
        Values = values.Select(ParseComposite).ToArray();
    }

    /// <summary>
    /// Initializes a new instance of the CompositeFiltering class with the specified operator and filtering instances.
    /// </summary>
    /// <param name="compositeOperator">The composite operator to use for combining the conditions.</param>
    /// <param name="values">Array of filtering instances to be combined.</param>
    public CompositeFiltering(CompositeOperator compositeOperator, params IFiltering[] values)
    {
        CompositeOperator = compositeOperator;
        Values = values;
    }

    /// <summary>
    /// Initializes a new instance of the CompositeFiltering class with a specific column name and filtering instances.
    /// This constructor is typically used for Any/All operations on collection properties.
    /// </summary>
    /// <param name="compositeOperator">The composite operator to use for combining the conditions.</param>
    /// <param name="columnName">The name of the column or property this composite filter applies to.</param>
    /// <param name="values">Array of filtering instances to be combined.</param>
    public CompositeFiltering(CompositeOperator compositeOperator, string columnName, params IFiltering[] values)
    {
        ColumnName = columnName;
        CompositeOperator = compositeOperator;
        Values = values;
    }

    /// <summary>
    /// Initializes a new instance of the CompositeFiltering class with a specific column name and string filter expressions.
    /// This constructor is typically used for Any/All operations on collection properties.
    /// </summary>
    /// <param name="compositeOperator">The composite operator to use for combining the conditions.</param>
    /// <param name="columnName">The name of the column or property this composite filter applies to.</param>
    /// <param name="values">Array of string filter expressions to be parsed and combined.</param>
    public CompositeFiltering(CompositeOperator compositeOperator, string columnName, params string[] values)
    {
        ColumnName = columnName;
        CompositeOperator = compositeOperator;
        Values = values.Select(ParseComposite).ToArray();
    }

    /// <summary>
    /// Initializes a new instance of the CompositeFiltering class by copying properties from an existing filtering instance.
    /// </summary>
    /// <param name="filtering">The existing filtering instance to copy from.</param>
    public CompositeFiltering(IFiltering filtering)
    {
        ColumnName = filtering.ColumnName;
        StringValue = filtering.StringValue;
        Operator = filtering.Operator;
    }

    /// <summary>
    /// Gets or sets the composite operator used to combine the filtering conditions.
    /// </summary>
    public CompositeOperator CompositeOperator { get; set; }
    
    /// <summary>
    /// Gets or sets the array of filtering conditions to be combined using the composite operator.
    /// </summary>
    public IFiltering[] Values { get; set; }

    /// <summary>
    /// Parses a query string into a filtering instance, supporting both regular and collection-based operations.
    /// </summary>
    /// <param name="query">The query string to parse. Supports formats like 'property:operator(value)' and 'collection:any/all(conditions)'.</param>
    /// <returns>A filtering instance representing the parsed query.</returns>
    /// <exception cref="InvalidFormatException">Thrown when the query format is invalid or unsupported.</exception>
    public static IFiltering ParseComposite(string query)
    {
        // First try to match collection:operator(args) format for Any/All
        var collectionMatch = Regex.Match(query, @"^(\w+):(any|all)\((.*)\)$");
        if (collectionMatch.Success && collectionMatch.Groups.Count == 4)
        {
            var collectionName = collectionMatch.Groups[1].Value;
            var operatorName = collectionMatch.Groups[2].Value;
            var arguments = collectionMatch.Groups[3].Value;
            
            var compositeOperator = CompositeOperatorValues.GetOperator(operatorName);
            if (compositeOperator != null)
            {
                var matches = Regex.Matches(arguments, ArgsRegex);
                return new CompositeFiltering(compositeOperator.Value, collectionName, matches.Select(m => m.Value.Trim()).ToArray());
            }
        }

        // Fall back to regular operator(args) format for And/Or
        var match = Regex.Match(query, FuncRegex);
        if (!match.Success || match.Groups.Count != 3)
        {
            return Parse(query);
        }

        var @operator = match.Groups[1].Value;
        var regularCompositeOperator = CompositeOperatorValues.GetOperator(@operator);

        if (regularCompositeOperator == null)
        {
            return Parse(query);
        }

        var regularArguments = match.Groups[2].Value;
        var regularMatches = Regex.Matches(regularArguments, ArgsRegex);

        return new CompositeFiltering(regularCompositeOperator.Value, regularMatches.Select(m => m.Value.Trim()).ToArray());
    }

    /// <summary>
    /// Converts the composite filtering instance to its string representation.
    /// For collection operations (Any/All), formats as 'collection:operator(args)'.
    /// For regular operations (And/Or), formats as 'operator(args)'.
    /// </summary>
    /// <returns>A string representation of the composite filtering operation.</returns>
    public override string ToString()
    {
        if (Values == null) return base.ToString();

        var args = Values.Select(v => v.ToString());
        var operatorStr = CompositeOperatorValues.GetOperator(CompositeOperator);
        var argsStr = string.Join(", ", args);
        
        // For Any/All operators with collection column name, format as: collection:any(args)
        if (!string.IsNullOrEmpty(ColumnName) && CompositeOperator is CompositeOperator.Any or CompositeOperator.All)
        {
            return $"{ColumnName}:{operatorStr}({argsStr})";
        }
        
        return $"{operatorStr}({argsStr})";
    }
}

/// <summary>
/// Represents a strongly-typed composite filtering operation that combines multiple filtering conditions using logical operators.
/// Provides type-safe access to entity properties through lambda expressions.
/// </summary>
/// <typeparam name="T">The type of entity being filtered.</typeparam>
public class CompositeFiltering<T> : CompositeFiltering, IFiltering<T>
{
    /// <summary>
    /// Gets the lambda expression that represents the property or column being filtered.
    /// </summary>
    public Expression<Func<T, object>> Expression { get; }

    /// <summary>
    /// Initializes a new instance of the strongly-typed CompositeFiltering class with string filter expressions.
    /// </summary>
    /// <param name="compositeOperator">The composite operator to use for combining the conditions.</param>
    /// <param name="values">Array of string filter expressions to be parsed and combined.</param>
    public CompositeFiltering(CompositeOperator compositeOperator, params string[] values) : base(compositeOperator, values)
    {
    }

    /// <summary>
    /// Initializes a new instance of the strongly-typed CompositeFiltering class with typed filtering instances.
    /// </summary>
    /// <param name="compositeOperator">The composite operator to use for combining the conditions.</param>
    /// <param name="values">Array of typed filtering instances to be combined.</param>
    public CompositeFiltering(CompositeOperator compositeOperator, params IFiltering<T>[] values) : base(compositeOperator, values)
    {
    }

    /// <summary>
    /// Initializes a new instance of the strongly-typed CompositeFiltering class with a lambda expression and string filter expressions.
    /// </summary>
    /// <param name="compositeOperator">The composite operator to use for combining the conditions.</param>
    /// <param name="expression">Lambda expression representing the property or collection to filter.</param>
    /// <param name="values">Array of string filter expressions to be parsed and combined.</param>
    public CompositeFiltering(CompositeOperator compositeOperator, Expression<Func<T, object>> expression, params string[] values) : base(compositeOperator, expression.GetColumnName(), values)
    {
    }

    /// <summary>
    /// Initializes a new instance of the strongly-typed CompositeFiltering class with a lambda expression and typed filtering instances.
    /// </summary>
    /// <param name="compositeOperator">The composite operator to use for combining the conditions.</param>
    /// <param name="expression">Lambda expression representing the property or collection to filter.</param>
    /// <param name="values">Array of typed filtering instances to be combined.</param>
    public CompositeFiltering(CompositeOperator compositeOperator, Expression<Func<T, object>> expression, params IFiltering<T>[] values) : base(compositeOperator, expression.GetColumnName(), values)
    {
    }

    /// <summary>
    /// Initializes a new instance of the strongly-typed CompositeFiltering class with a lambda expression, column fallback option, and string filter expressions.
    /// </summary>
    /// <param name="compositeOperator">The composite operator to use for combining the conditions.</param>
    /// <param name="expression">Lambda expression representing the property or collection to filter.</param>
    /// <param name="useColumnFallback">Whether to use column attribute fallback when resolving property names.</param>
    /// <param name="values">Array of string filter expressions to be parsed and combined.</param>
    public CompositeFiltering(CompositeOperator compositeOperator, Expression<Func<T, object>> expression, bool useColumnFallback, params string[] values) : base(compositeOperator, expression.GetColumnName(useColumnFallback), values)
    {
    }

    /// <summary>
    /// Initializes a new instance of the strongly-typed CompositeFiltering class with a lambda expression, column fallback option, and typed filtering instances.
    /// </summary>
    /// <param name="compositeOperator">The composite operator to use for combining the conditions.</param>
    /// <param name="expression">Lambda expression representing the property or collection to filter.</param>
    /// <param name="useColumnFallback">Whether to use column attribute fallback when resolving property names.</param>
    /// <param name="values">Array of typed filtering instances to be combined.</param>
    public CompositeFiltering(CompositeOperator compositeOperator, Expression<Func<T, object>> expression, bool useColumnFallback, params IFiltering<T>[] values) : base(compositeOperator, expression.GetColumnName(useColumnFallback), values)
    {
    }

    /// <summary>
    /// Initializes a new instance of the strongly-typed CompositeFiltering class by copying from an existing typed filtering instance.
    /// </summary>
    /// <param name="filtering">The existing typed filtering instance to copy from.</param>
    public CompositeFiltering(IFiltering<T> filtering) : base(filtering)
    {
        Expression = filtering.Expression;
    }

    /// <summary>
    /// Sets the column name based on the provided lambda expression.
    /// </summary>
    /// <param name="expression">Lambda expression representing the property or collection.</param>
    /// <param name="useColumnFallback">Whether to use column attribute fallback when resolving property names.</param>
    public void SetColumn(Expression<Func<T, object>> expression, bool useColumnFallback = false)
    {
        ColumnName = expression.GetColumnName(useColumnFallback);
    }
}
