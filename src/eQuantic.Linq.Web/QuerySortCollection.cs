namespace eQuantic.Linq.Web;

/// <summary>
/// Typed sort collection for request contracts: items are <see cref="QuerySort{TEntity}"/>
/// entries parsed from the eQuantic.Linq syntax (<c>path</c> or <c>path:desc</c>,
/// comma-separated). The static <see cref="TryParse"/> makes the type bind natively from the
/// query string in both ASP.NET Core pipelines (MVC properties/parameters and Minimal APIs)
/// with no model binder and no framework dependency in this package.
/// </summary>
/// <typeparam name="TEntity">Root entity the sort expressions are anchored on.</typeparam>
public class QuerySortCollection<TEntity> : List<QuerySort<TEntity>>
{
    public QuerySortCollection()
    {
    }

    public QuerySortCollection(IEnumerable<QuerySort<TEntity>> collection) : base(collection)
    {
    }

    /// <summary>Parses a single query-string value (the binding contract for both pipelines).</summary>
    /// <param name="value">Raw ordering expression.</param>
    /// <param name="provider">Unused; required by the binding contract.</param>
    /// <param name="sortCollection">Parsed collection, or null when the value is invalid or empty.</param>
    public static bool TryParse(string? value, IFormatProvider? provider,
        out QuerySortCollection<TEntity>? sortCollection)
    {
        if (TryParseValue(value, out var sorts))
        {
            sortCollection = new QuerySortCollection<TEntity>(sorts);
            return true;
        }

        sortCollection = null;
        return false;
    }

    /// <summary>Parses one raw ordering value (shared by derived collections).</summary>
    /// <param name="value">Raw ordering expression.</param>
    /// <param name="sorts">Parsed sorts on success.</param>
    protected static bool TryParseValue(string? value, out IReadOnlyList<QuerySort<TEntity>> sorts)
    {
        sorts = [];
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            sorts = QuerySort<TEntity>.Parse(value!);
            return true;
        }
        catch (QueryStringParseException)
        {
            return false;
        }
    }
}
