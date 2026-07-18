using Microsoft.AspNetCore.Http;

namespace eQuantic.Linq.Web.AspNetCore;

/// <summary>Binding-free access to entity queries from HTTP requests.</summary>
public static class HttpRequestQueryExtensions
{
    /// <summary>Parses the request's query string into an <see cref="EntityQuery{T}"/>.</summary>
    /// <typeparam name="T">Root entity type.</typeparam>
    /// <param name="request">Current HTTP request.</param>
    /// <param name="options">Syntax options; falls back to the DI-registered <see cref="QueryStringOptions"/>, then defaults.</param>
    public static EntityQuery<T> ParseEntityQuery<T>(this HttpRequest request, QueryStringOptions? options = null)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        return QueryStringHttpParser.Parse<T>(request.HttpContext, options);
    }
}
