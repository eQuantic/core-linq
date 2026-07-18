using System.Reflection;
using Microsoft.AspNetCore.Http;

namespace eQuantic.Linq.Web.AspNetCore;

/// <summary>
/// Minimal-API binding wrapper for <see cref="EntityQuery{T}"/>: declare it as a handler parameter and
/// the request's query string (<c>filter</c>, <c>orderBy</c>, <c>skip</c>, <c>take</c>, <c>select</c>)
/// is parsed automatically. Invalid syntax becomes an HTTP 400.
/// </summary>
/// <example>
/// <code>
/// app.MapGet("/orders", (EntityQueryModel&lt;Order&gt; query, AppDb db) => query.Apply(db.Orders));
/// </code>
/// </example>
/// <typeparam name="T">Root entity type.</typeparam>
public sealed class EntityQueryModel<T>
{
    /// <summary>Creates the wrapper around a parsed query.</summary>
    /// <param name="query">Parsed entity query.</param>
    public EntityQueryModel(EntityQuery<T> query)
    {
        Query = query ?? throw new ArgumentNullException(nameof(query));
    }

    /// <summary>The parsed entity query.</summary>
    public EntityQuery<T> Query { get; }

    /// <summary>Unwraps the parsed query.</summary>
    /// <param name="model">Wrapper to unwrap.</param>
    public static implicit operator EntityQuery<T>(EntityQueryModel<T> model) => model.Query;

    /// <summary>Applies filter, ordering and paging to the source (see <see cref="EntityQuery{T}.Apply"/>).</summary>
    /// <param name="source">Queryable source.</param>
    public IQueryable<T> Apply(IQueryable<T> source) => Query.Apply(source);

    /// <summary>Applies the full query including the projection (see <see cref="EntityQuery{T}.ApplyWithSelection"/>).</summary>
    /// <param name="source">Queryable source.</param>
    public IQueryable ApplyWithSelection(IQueryable<T> source) => Query.ApplyWithSelection(source);

    /// <summary>Minimal-API binding hook: parses the request query string.</summary>
    /// <param name="context">Current HTTP context.</param>
    /// <param name="parameter">Handler parameter being bound.</param>
    /// <exception cref="BadHttpRequestException">The query string is invalid (HTTP 400).</exception>
    public static ValueTask<EntityQueryModel<T>?> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        try
        {
            return new ValueTask<EntityQueryModel<T>?>(
                new EntityQueryModel<T>(QueryStringHttpParser.Parse<T>(context)));
        }
        catch (Exception exception) when (QueryStringHttpParser.IsClientError(exception))
        {
            throw new BadHttpRequestException(
                $"Invalid query string: {exception.Message}",
                StatusCodes.Status400BadRequest,
                exception);
        }
    }
}
