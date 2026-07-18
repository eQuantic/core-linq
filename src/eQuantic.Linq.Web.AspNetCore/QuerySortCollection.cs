using System.Reflection;
using Microsoft.AspNetCore.Http;

namespace eQuantic.Linq.Web.AspNetCore;

/// <summary>
/// Typed sort collection bound from the query string: values are parsed with the eQuantic.Linq
/// syntax (<c>path</c> or <c>path:desc</c>, comma-separated) into <see cref="QuerySort{TEntity}"/>
/// items. Binds natively in MVC and Minimal APIs — <see cref="TryParse"/> serves attribute-bound
/// members (<c>[FromQuery(Name = "…")]</c> decides the key) and <see cref="BindAsync"/> serves
/// direct Minimal API parameters (key from <c>[FromQuery(Name = "…")]</c> when present, otherwise
/// the parameter name).
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

    /// <summary>Parses a single query-string value (used by MVC and attribute-bound members).</summary>
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

    /// <summary>
    /// Binds every query value of the parameter's key (used by direct Minimal API parameters).
    /// </summary>
    /// <param name="context">Current HTTP context.</param>
    /// <param name="parameter">Bound parameter info; <c>[FromQuery(Name = "…")]</c> selects the key.</param>
    public static ValueTask<QuerySortCollection<TEntity>?> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        var collection = new QuerySortCollection<TEntity>();
        foreach (var value in context.Request.Query[QueryBindingKeys.Resolve(parameter, "orderBy")])
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                collection.AddRange(QuerySort<TEntity>.Parse(value!));
            }
        }

        return new ValueTask<QuerySortCollection<TEntity>?>(collection);
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
