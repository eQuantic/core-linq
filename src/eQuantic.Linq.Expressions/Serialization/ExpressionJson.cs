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
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals | JsonNumberHandling.AllowReadingFromString,
            AllowOutOfOrderMetadataProperties = true,
            WriteIndented = options.WriteIndented,

            // Expression trees nest deeply (every binary node ≈ 3 JSON levels); the STJ default of 64
            // would reject legitimate wide filters.
            MaxDepth = 512,
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
