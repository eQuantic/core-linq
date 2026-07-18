using System.Linq.Expressions;
using eQuantic.Linq.Expressions;
using eQuantic.Linq.Expressions.Nodes;
using eQuantic.Linq.Web.Syntax;

namespace eQuantic.Linq.Web;

/// <summary>
/// Parses filter expressions (<c>property:op(value)</c>, <c>and</c>/<c>or</c>/<c>not</c>,
/// <c>any</c>/<c>all</c>, aggregates) into root-anchored expression models and typed predicates.
/// </summary>
public static class QueryFilter
{
    /// <summary>Parses a filter expression into a root-anchored model (transport-friendly, JSON-serializable).</summary>
    /// <typeparam name="T">Root entity type.</typeparam>
    /// <param name="filter">Filter expression, e.g. <c>total:gt(100),items:any(price:gt(50))</c>.</param>
    /// <param name="options">Syntax options; defaults apply when omitted.</param>
    public static ExpressionModel<T> ParseModel<T>(string filter, QueryStringOptions? options = null)
    {
        options ??= QueryStringOptions.Default;

        var body = FilterSyntaxParser.ParseBody(filter, options.RootParameterName);

        return new ExpressionModel<T>
        {
            Parameters = [new ParameterNode { Name = options.RootParameterName }],
            Body = body,
        };
    }

    /// <summary>Parses a filter expression directly into a typed predicate.</summary>
    /// <typeparam name="T">Root entity type.</typeparam>
    /// <param name="filter">Filter expression.</param>
    /// <param name="options">Syntax options; defaults apply when omitted.</param>
    public static Expression<Func<T, bool>> Parse<T>(string filter, QueryStringOptions? options = null)
    {
        options ??= QueryStringOptions.Default;
        return options.GetOrAddFilter<T>(filter, f => options.Serializer.ToPredicate(ParseModel<T>(f, options)));
    }
}
