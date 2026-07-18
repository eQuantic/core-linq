using System.Linq.Expressions;
using System.Reflection;
using eQuantic.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace eQuantic.Linq.Web.AspNetCore;

/// <summary>
/// Typed filter collection bound from the query string: each value is parsed with the
/// eQuantic.Linq syntax (e.g. <c>total:gt(100),status:eq(Paid)</c>) into a serializable
/// <see cref="ExpressionModel{TEntity}"/>; items combine with AND. Binds natively in MVC and
/// Minimal APIs — <see cref="TryParse"/> serves attribute-bound members
/// (<c>[FromQuery(Name = "…")]</c> decides the key) and <see cref="BindAsync"/> serves direct
/// Minimal API parameters (key from <c>[FromQuery(Name = "…")]</c> when present, otherwise the
/// parameter name).
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

    /// <summary>Parses a single query-string value (used by MVC and attribute-bound members).</summary>
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

    /// <summary>
    /// Binds every query value of the parameter's key (used by direct Minimal API parameters).
    /// </summary>
    /// <param name="context">Current HTTP context.</param>
    /// <param name="parameter">Bound parameter info; <c>[FromQuery(Name = "…")]</c> selects the key.</param>
    public static ValueTask<QueryFilterCollection<TEntity>?> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        var collection = new QueryFilterCollection<TEntity>();
        foreach (var value in context.Request.Query[QueryBindingKeys.Resolve(parameter, "filter")])
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                collection.Add(QueryFilter.ParseModel<TEntity>(value!));
            }
        }

        return new ValueTask<QueryFilterCollection<TEntity>?>(collection);
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

/// <summary>Key resolution shared by the query-collection binders.</summary>
internal static class QueryBindingKeys
{
    public static string Resolve(ParameterInfo parameter, string fallback) =>
        parameter.GetCustomAttribute<FromQueryAttribute>()?.Name
        ?? parameter.Name
        ?? fallback;
}
