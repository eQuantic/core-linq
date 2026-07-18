using System.Linq.Expressions;
using eQuantic.Linq.Expressions;
using eQuantic.Linq.Expressions.Nodes;
using eQuantic.Linq.Web.Syntax;

namespace eQuantic.Linq.Web;

/// <summary>One parsed ordering segment: a key selector over the root entity plus a direction.</summary>
/// <typeparam name="T">Root entity type.</typeparam>
public sealed class QuerySort<T>
{
    internal QuerySort(string path, SortDirection direction, LambdaExpression keySelector)
    {
        Path = path;
        Direction = direction;
        KeySelector = keySelector;
    }

    /// <summary>Original path text (e.g. <c>customer.name</c>).</summary>
    public string Path { get; }

    /// <summary>Sort direction.</summary>
    public SortDirection Direction { get; }

    /// <summary>Typed key selector (<c>T → TKey</c>).</summary>
    public LambdaExpression KeySelector { get; }

    /// <summary>
    /// Parses an ordering expression, e.g. <c>total:desc,customer.name</c>
    /// (direction defaults to ascending; paths support navigation and method segments).
    /// </summary>
    /// <param name="orderBy">Ordering expression.</param>
    /// <param name="options">Syntax options; defaults apply when omitted.</param>
    public static IReadOnlyList<QuerySort<T>> Parse(string orderBy, QueryStringOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(orderBy))
        {
            throw new ArgumentException("Ordering expression is empty.", nameof(orderBy));
        }

        options ??= QueryStringOptions.Default;

        var reader = new SyntaxReader(orderBy);
        var sorts = new List<QuerySort<T>>();

        while (true)
        {
            reader.SkipWhitespace();
            var start = reader.Position;
            var node = FilterSyntaxParser.ParsePath(reader, options.RootParameterName, depth: 0);
            var pathText = orderBy.Substring(start, reader.Position - start).Trim();

            var direction = SortDirection.Ascending;
            if (reader.TryConsume(':'))
            {
                var directionText = reader.ReadIdentifier();
                direction = directionText.ToLowerInvariant() switch
                {
                    "asc" or "ascending" => SortDirection.Ascending,
                    "desc" or "descending" => SortDirection.Descending,
                    _ => throw reader.Error($"unknown sort direction '{directionText}' (expected asc or desc)"),
                };
            }

            var model = new ExpressionModel
            {
                Parameters = [new ParameterNode { Name = options.RootParameterName }],
                Body = node,
            };

            sorts.Add(new QuerySort<T>(pathText, direction, options.Serializer.ToLambda(model, typeof(T))));

            if (!reader.TryConsume(','))
            {
                break;
            }
        }

        if (!reader.End)
        {
            throw reader.Error("unexpected trailing content");
        }

        return sorts;
    }
}
