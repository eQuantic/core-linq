using eQuantic.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace eQuantic.Linq.Web.AspNetCore;

/// <summary>Shared parsing plumbing between the MVC binder, the minimal-API wrapper and the request extensions.</summary>
internal static class QueryStringHttpParser
{
    public static IEnumerable<KeyValuePair<string, string>> Pairs(IQueryCollection query)
    {
        foreach (var entry in query)
        {
            foreach (var value in entry.Value)
            {
                yield return new KeyValuePair<string, string>(entry.Key, value ?? string.Empty);
            }
        }
    }

    public static QueryStringOptions ResolveOptions(HttpContext context, QueryStringOptions? options) =>
        options
        ?? context.RequestServices.GetService<QueryStringOptions>()
        ?? new QueryStringOptions();

    public static EntityQuery<T> Parse<T>(HttpContext context, QueryStringOptions? options = null) =>
        EntityQuery.Parse<T>(Pairs(context.Request.Query), ResolveOptions(context, options));

    /// <summary>Whether the exception represents invalid client input (→ HTTP 400) rather than a server fault.</summary>
    public static bool IsClientError(Exception exception) =>
        exception is QueryStringParseException or ExpressionSerializationException or ArgumentException;
}
