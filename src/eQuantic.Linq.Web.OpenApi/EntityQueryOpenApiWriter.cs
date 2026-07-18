using eQuantic.Linq.Web.Documentation;
using Microsoft.OpenApi;

namespace eQuantic.Linq.Web.OpenApi;

/// <summary>Mutates an OpenAPI operation with the entity-query parameter documentation.</summary>
internal static class EntityQueryOpenApiWriter
{
    private static readonly string[] ShadowNames = ["query", "filterModel", "filter", "sorts", "skip", "take", "selector"];

    public static void Document(OpenApiOperation operation, string boundParameterName, Type entityType, QueryStringOptions options)
    {
        var documentation = EntityQueryDocumentation.For(entityType, options);

        operation.Parameters ??= new List<IOpenApiParameter>();

        for (var i = operation.Parameters.Count - 1; i >= 0; i--)
        {
            var name = operation.Parameters[i].Name;
            if (string.Equals(name, boundParameterName, StringComparison.OrdinalIgnoreCase) ||
                ShadowNames.Any(s => string.Equals(s, name, StringComparison.OrdinalIgnoreCase)))
            {
                operation.Parameters.RemoveAt(i);
            }
        }

        foreach (var parameter in documentation.Parameters)
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = parameter.Name,
                In = ParameterLocation.Query,
                Required = false,
                Description = parameter.Description,
                Schema = new OpenApiSchema
                {
                    Type = parameter.IsInteger ? JsonSchemaType.Integer : JsonSchemaType.String,
                },
                Example = parameter.Example is null
                    ? null
                    : System.Text.Json.Nodes.JsonValue.Create(parameter.Example),
            });
        }

        operation.Responses ??= new OpenApiResponses();
        if (!operation.Responses.ContainsKey("400"))
        {
            operation.Responses["400"] = new OpenApiResponse
            {
                Description = "Invalid query syntax — the parse error (with position) is returned in the response body.",
            };
        }
    }
}
