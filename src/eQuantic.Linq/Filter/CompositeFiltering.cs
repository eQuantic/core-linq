using System.Linq.Expressions;
using System.Text.RegularExpressions;
using eQuantic.Linq.Extensions;

namespace eQuantic.Linq.Filter;

public enum CompositeOperator
{
    And,
    Or
}

public class CompositeFiltering : Filtering
{
    public CompositeFiltering(CompositeOperator compositeOperator, params string[] values)
    {
        CompositeOperator = compositeOperator;
        Values = values.Select(ParseComposite).ToArray();
    }

    public CompositeFiltering(CompositeOperator compositeOperator, params IFiltering[] values)
    {
        CompositeOperator = compositeOperator;
        Values = values;
    }

    public CompositeFiltering(IFiltering filtering)
    {
        ColumnName = filtering.ColumnName;
        StringValue = filtering.StringValue;
        Operator = filtering.Operator;
    }

    public CompositeOperator CompositeOperator { get; set; }
    public IFiltering[] Values { get; set; }

    public static IFiltering ParseComposite(string query)
    {
        var match = Regex.Match(query, FuncRegex);
        if (!match.Success || match.Groups.Count != 3)
        {
            return Parse(query);
        }

        var @operator = match.Groups[1].Value;
        var compositeOperator = CompositeOperatorValues.GetOperator(@operator);

        if (compositeOperator == null)
        {
            return Parse(query);
        }

        var arguments = match.Groups[2].Value;
        var matches = Regex.Matches(arguments, ArgsRegex);

        return new CompositeFiltering(compositeOperator.Value, matches.Select(m => m.Value.Trim()).ToArray());
    }

    public override string ToString()
    {
        if (Values == null) return base.ToString();

        var args = Values.Select(v => v.ToString());
        return $"{CompositeOperatorValues.GetOperator(CompositeOperator)}({string.Join(", ", args)})";
    }
}

public class CompositeFiltering<T> : CompositeFiltering, IFiltering<T>
{
    public Expression<Func<T, object>> Expression { get; }
        
    public CompositeFiltering(CompositeOperator compositeOperator, params string[] values) : base(compositeOperator, values)
    {
    }

    public CompositeFiltering(CompositeOperator compositeOperator, params IFiltering<T>[] values) : base(compositeOperator, values)
    {
    }

    public CompositeFiltering(IFiltering<T> filtering) : base(filtering)
    {
        Expression = filtering.Expression;
    }

    public void SetColumn(Expression<Func<T, object>> expression, bool useColumnFallback = false)
    {
        ColumnName = expression.GetColumnName(useColumnFallback);
    }
}