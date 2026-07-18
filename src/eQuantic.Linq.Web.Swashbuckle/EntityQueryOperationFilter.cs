using eQuantic.Linq.Web.AspNetCore;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace eQuantic.Linq.Web.Swashbuckle;

/// <summary>
/// Documents endpoints that bind <see cref="EntityQuery{T}"/> (MVC) or
/// <see cref="EntityQueryModel{T}"/> (Minimal APIs): replaces the opaque bound parameter with the
/// five query-string parameters (<c>filter</c>, <c>orderBy</c>, <c>skip</c>, <c>take</c>,
/// <c>select</c>), each described with the complete syntax, the entity's actual member paths and
/// generated examples, and ensures the 400 parse-error response is declared.
/// </summary>
public sealed class EntityQueryOperationFilter : IOperationFilter
{
    private readonly QueryStringOptions _options;

    /// <summary>Creates the filter.</summary>
    /// <param name="options">Query-string options; key names are honored. Defaults apply when omitted.</param>
    public EntityQueryOperationFilter(QueryStringOptions? options = null) => _options = options ?? new QueryStringOptions();

    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        foreach (var parameter in context.ApiDescription.ParameterDescriptions)
        {
            if (TryGetEntityType(parameter.Type, out var entityType))
            {
                EntityQueryOpenApiWriter.Document(operation, parameter.Name, entityType, _options);
            }
        }
    }

    private static bool TryGetEntityType(Type? parameterType, out Type entityType)
    {
        entityType = typeof(object);
        if (parameterType is not { IsGenericType: true })
        {
            return false;
        }

        var definition = parameterType.GetGenericTypeDefinition();
        if (definition != typeof(EntityQuery<>) && definition != typeof(EntityQueryModel<>))
        {
            return false;
        }

        entityType = parameterType.GetGenericArguments()[0];
        return true;
    }
}
