using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace eQuantic.Linq.Web.Swashbuckle;

/// <summary>Swashbuckle registration for eQuantic.Linq query-string documentation.</summary>
public static class SwaggerGenOptionsExtensions
{
    /// <summary>
    /// Documents every endpoint binding <see cref="EntityQuery{T}"/> / <see cref="AspNetCore.EntityQueryModel{T}"/>:
    /// the five query-string parameters with full syntax, entity member paths and examples.
    /// </summary>
    /// <param name="options">Swagger generation options.</param>
    /// <param name="queryStringOptions">
    /// Query-string options; pass your configured instance so customized key names are reflected.
    /// Defaults apply when omitted.
    /// </param>
    public static SwaggerGenOptions AddEntityQueryDocumentation(
        this SwaggerGenOptions options, QueryStringOptions? queryStringOptions = null)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        options.OperationFilter<EntityQueryOperationFilter>(queryStringOptions ?? new QueryStringOptions());
        return options;
    }
}
