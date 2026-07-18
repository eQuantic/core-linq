using eQuantic.Linq.Web.AspNetCore;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace eQuantic.Linq.Web.OpenApi;

/// <summary>
/// Documents endpoints that bind <see cref="EntityQuery{T}"/> (MVC) or
/// <see cref="EntityQueryModel{T}"/> (Minimal APIs): replaces the opaque bound parameter with the
/// five query-string parameters (<c>filter</c>, <c>orderBy</c>, <c>skip</c>, <c>take</c>,
/// <c>select</c>), each described with the complete syntax, the entity's actual member paths and
/// generated examples, and ensures the 400 parse-error response is declared.
/// </summary>
public sealed class EntityQueryOperationTransformer : IOpenApiOperationTransformer
{
    private readonly QueryStringOptions? _options;

    /// <summary>Creates the transformer.</summary>
    /// <param name="options">
    /// Query-string options; key names are honored. When omitted, the options registered in DI
    /// (via <c>AddEntityQueryBinding</c>) are used, falling back to defaults.
    /// </param>
    public EntityQueryOperationTransformer(QueryStringOptions? options = null) => _options = options;

    /// <inheritdoc />
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var options = _options
                      ?? context.ApplicationServices.GetService<QueryStringOptions>()
                      ?? new QueryStringOptions();

        foreach (var parameter in context.Description.ParameterDescriptions)
        {
            if (TryGetEntityType(parameter.Type, out var entityType))
            {
                EntityQueryOpenApiWriter.Document(operation, parameter.Name, entityType, options);
            }
        }

        return Task.CompletedTask;
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
