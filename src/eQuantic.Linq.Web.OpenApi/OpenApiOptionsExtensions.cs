using Microsoft.AspNetCore.OpenApi;

namespace eQuantic.Linq.Web.OpenApi;

/// <summary>Microsoft.AspNetCore.OpenApi registration for eQuantic.Linq query-string documentation.</summary>
public static class OpenApiOptionsExtensions
{
    /// <summary>
    /// Documents every endpoint binding <see cref="EntityQuery{T}"/> / <see cref="AspNetCore.EntityQueryModel{T}"/>:
    /// the five query-string parameters with full syntax, entity member paths and examples.
    /// </summary>
    /// <param name="options">OpenAPI document options (from <c>AddOpenApi(o =&gt; …)</c>).</param>
    /// <param name="queryStringOptions">
    /// Query-string options; pass your configured instance so customized key names are reflected.
    /// When omitted, the options registered in DI (via <c>AddEntityQueryBinding</c>) are used,
    /// falling back to defaults.
    /// </param>
    public static OpenApiOptions AddEntityQueryDocumentation(
        this OpenApiOptions options, QueryStringOptions? queryStringOptions = null)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        return options.AddOperationTransformer(new EntityQueryOperationTransformer(queryStringOptions));
    }
}
