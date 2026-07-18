using eQuantic.Linq.Expressions;

namespace eQuantic.Linq.Web;

/// <summary>One serializable ordering segment of a <see cref="QueryModel{T}"/>.</summary>
public class QuerySortModel
{
    /// <summary>Key selector as a root-anchored expression model.</summary>
    public ExpressionModel Key { get; set; } = null!;

    /// <summary>Sort direction.</summary>
    public SortDirection Direction { get; set; }

    /// <summary>Original path text, for diagnostics.</summary>
    public string? Path { get; set; }
}

/// <summary>
/// The whole entity query as ONE serializable document — filter, ordering, paging and projection —
/// so a query received on a web endpoint can be forwarded between services as JSON and rebuilt anywhere.
/// </summary>
/// <typeparam name="T">Root entity type.</typeparam>
public class QueryModel<T>
{
    /// <summary>Filter model (null when absent).</summary>
    public ExpressionModel<T>? Filter { get; set; }

    /// <summary>Ordering segments, in precedence order.</summary>
    public List<QuerySortModel>? OrderBy { get; set; }

    /// <summary>Number of elements to skip.</summary>
    public int? Skip { get; set; }

    /// <summary>Number of elements to take.</summary>
    public int? Take { get; set; }

    /// <summary>Projection selector model (null when absent).</summary>
    public ExpressionModel? Select { get; set; }

    /// <summary>Rebuilds an applicable <see cref="EntityQuery{T}"/> from this document.</summary>
    /// <param name="options">Syntax options; defaults apply when omitted.</param>
    public EntityQuery<T> ToEntityQuery(QueryStringOptions? options = null)
    {
        options ??= QueryStringOptions.Default;
        var serializer = options.Serializer;

        var filter = Filter is null ? null : serializer.ToPredicate(Filter);

        IReadOnlyList<QuerySort<T>> sorts = OrderBy is { Count: > 0 }
            ? OrderBy
                .Select(entry => new QuerySort<T>(entry.Path ?? string.Empty, entry.Direction, serializer.ToLambda(entry.Key, typeof(T))))
                .ToList()
            : [];

        var selector = Select is null ? null : serializer.ToLambda(Select, typeof(T));

        return new EntityQuery<T>(Filter, filter, sorts, Skip, Take, selector, options);
    }
}
