using System.Text.Json;
using System.Text.Json.Serialization;

namespace eQuantic.Linq.Expressions.Serialization;

/// <summary>Builds the <see cref="JsonSerializerOptions"/> used for the expression model.</summary>
internal static class ExpressionJson
{
    public static JsonSerializerOptions CreateOptions(ExpressionSerializerOptions options)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            AllowOutOfOrderMetadataProperties = true,
            WriteIndented = options.WriteIndented,
        };

        jsonOptions.Converters.Add(new JsonStringEnumConverter());
#if !NETSTANDARD2_0
        jsonOptions.Converters.Add(new IndexJsonConverter());
        jsonOptions.Converters.Add(new RangeJsonConverter());
#endif

        options.ConfigureJson?.Invoke(jsonOptions);
        return jsonOptions;
    }
}
