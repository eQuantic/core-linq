using System.Linq.Expressions;
using System.Text;
using eQuantic.Linq.Expressions;

namespace eQuantic.Linq.Web;

/// <summary>Entry points for building typed query-string sort expressions in code.</summary>
public static class QuerySortBuilder
{
    /// <summary>Starts an empty sort builder for <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">Root entity being ordered.</typeparam>
    public static QuerySortBuilder<T> For<T>() => new();

    /// <summary>Parses an ordering expression (<c>total:desc,customer.name</c>) into a builder.</summary>
    /// <typeparam name="T">Root entity being ordered.</typeparam>
    /// <param name="orderBy">Ordering expression.</param>
    /// <param name="options">Query-string options; defaults apply when omitted.</param>
    public static QuerySortBuilder<T> Parse<T>(string orderBy, QueryStringOptions? options = null) =>
        new(QuerySort<T>.Parse(orderBy, options));
}

/// <summary>
/// Builds query-string sort expressions from typed member selectors and round-trips them:
/// <c>ToString()</c> produces the <c>orderBy</c> value (<c>total:desc,customer.name</c>), and
/// <see cref="QuerySortBuilder.Parse{T}"/> reads one back into a builder. Fills the gap left by the
/// internal <see cref="QuerySort{T}"/> constructor — sorts can once again be authored in code.
/// </summary>
/// <typeparam name="T">Root entity being ordered.</typeparam>
public sealed class QuerySortBuilder<T>
{
    private readonly List<QuerySort<T>> _sorts = [];

    internal QuerySortBuilder()
    {
    }

    internal QuerySortBuilder(IEnumerable<QuerySort<T>> sorts) => _sorts.AddRange(sorts);

    /// <summary>Appends an ascending sort by the selected member.</summary>
    /// <typeparam name="TKey">Member type.</typeparam>
    /// <param name="selector">Member selector, e.g. <c>o =&gt; o.Customer.Name</c>.</param>
    public QuerySortBuilder<T> By<TKey>(Expression<Func<T, TKey>> selector) =>
        Add(selector, SortDirection.Ascending);

    /// <summary>Appends a descending sort by the selected member.</summary>
    /// <typeparam name="TKey">Member type.</typeparam>
    /// <param name="selector">Member selector.</param>
    public QuerySortBuilder<T> ByDescending<TKey>(Expression<Func<T, TKey>> selector) =>
        Add(selector, SortDirection.Descending);

    /// <summary>Appends an ascending sort (reads as a secondary sort after previous calls).</summary>
    /// <typeparam name="TKey">Member type.</typeparam>
    /// <param name="selector">Member selector.</param>
    public QuerySortBuilder<T> ThenBy<TKey>(Expression<Func<T, TKey>> selector) =>
        Add(selector, SortDirection.Ascending);

    /// <summary>Appends a descending sort (reads as a secondary sort after previous calls).</summary>
    /// <typeparam name="TKey">Member type.</typeparam>
    /// <param name="selector">Member selector.</param>
    public QuerySortBuilder<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> selector) =>
        Add(selector, SortDirection.Descending);

    /// <summary>Appends an ascending sort by path string, e.g. <c>"customer.name"</c>.</summary>
    /// <param name="path">Member path.</param>
    public QuerySortBuilder<T> By(string path) => Add(path, SortDirection.Ascending);

    /// <summary>Appends a descending sort by path string.</summary>
    /// <param name="path">Member path.</param>
    public QuerySortBuilder<T> ByDescending(string path) => Add(path, SortDirection.Descending);

    /// <summary>Appends an ascending sort by path string (reads as a secondary sort).</summary>
    /// <param name="path">Member path.</param>
    public QuerySortBuilder<T> ThenBy(string path) => Add(path, SortDirection.Ascending);

    /// <summary>Appends a descending sort by path string (reads as a secondary sort).</summary>
    /// <param name="path">Member path.</param>
    public QuerySortBuilder<T> ThenByDescending(string path) => Add(path, SortDirection.Descending);

    /// <summary>The built sorts, ready for <c>QueryOptions.OrderBy(params QuerySort&lt;T&gt;[])</c> and the like.</summary>
    public IReadOnlyList<QuerySort<T>> ToSorts() => _sorts;

    /// <summary>The <c>orderBy</c> query-string value (ascending direction is omitted, matching the parser default).</summary>
    public override string ToString()
    {
        var builder = new StringBuilder();
        for (var i = 0; i < _sorts.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            builder.Append(_sorts[i].Path);
            if (_sorts[i].Direction == SortDirection.Descending)
            {
                builder.Append(":desc");
            }
        }

        return builder.ToString();
    }

    private QuerySortBuilder<T> Add<TKey>(Expression<Func<T, TKey>> selector, SortDirection direction)
    {
        if (selector is null)
        {
            throw new ArgumentNullException(nameof(selector));
        }

        _sorts.Add(new QuerySort<T>(QueryLiteral.Path(selector), direction, selector));
        return this;
    }

    private QuerySortBuilder<T> Add(string path, SortDirection direction)
    {
        var raw = QueryLiteral.RawPath(path);
        _sorts.Add(new QuerySort<T>(raw, direction, MemberPathExtensions.ToSelector<T>(raw)));
        return this;
    }
}
