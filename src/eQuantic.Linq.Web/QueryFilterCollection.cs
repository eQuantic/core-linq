using System.Linq.Expressions;
using eQuantic.Linq.Expressions;

namespace eQuantic.Linq.Web;

/// <summary>
/// Typed filter collection for request contracts: each item is a serializable
/// <see cref="ExpressionModel{TEntity}"/> parsed from the eQuantic.Linq syntax
/// (e.g. <c>total:gt(100),status:eq(Paid)</c>); items combine with AND. The static
/// <see cref="TryParse"/> makes the type bind natively from the query string in both ASP.NET
/// Core pipelines (MVC properties/parameters and Minimal APIs) with no model binder and no
/// framework dependency in this package.
/// </summary>
/// <typeparam name="TEntity">Root entity the filter expressions are anchored on.</typeparam>
public class QueryFilterCollection<TEntity> : List<ExpressionModel<TEntity>>
{
    public QueryFilterCollection()
    {
    }

    public QueryFilterCollection(IEnumerable<ExpressionModel<TEntity>> collection) : base(collection)
    {
    }

    /// <summary>
    /// Combines all filter models into a single typed predicate (null when empty).
    /// </summary>
    /// <param name="options">Query-string options; defaults apply when omitted.</param>
    public Expression<Func<TEntity, bool>>? ToPredicate(QueryStringOptions? options = null)
    {
        var serializer = options?.Serializer ?? ExpressionSerializer.Default;
        Expression<Func<TEntity, bool>>? predicate = null;
        foreach (var model in this)
        {
            var next = serializer.ToPredicate(model);
            predicate = predicate is null ? next : predicate.AndAlso(next);
        }

        return predicate;
    }

    /// <summary>Parses a single query-string value (the binding contract for both pipelines).</summary>
    /// <param name="value">Raw filter expression.</param>
    /// <param name="provider">Unused; required by the binding contract.</param>
    /// <param name="filterCollection">Parsed collection, or null when the value is invalid or empty.</param>
    public static bool TryParse(string? value, IFormatProvider? provider,
        out QueryFilterCollection<TEntity>? filterCollection)
    {
        if (TryParseValue(value, out var model))
        {
            filterCollection = [model];
            return true;
        }

        filterCollection = null;
        return false;
    }

    /// <summary>Parses one raw filter value into a model (shared by derived collections).</summary>
    /// <param name="value">Raw filter expression.</param>
    /// <param name="model">Parsed model on success.</param>
    protected static bool TryParseValue(string? value, out ExpressionModel<TEntity> model)
    {
        model = null!;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            model = QueryFilter.ParseModel<TEntity>(value!);
            return true;
        }
        catch (QueryStringParseException)
        {
            return false;
        }
    }
}
