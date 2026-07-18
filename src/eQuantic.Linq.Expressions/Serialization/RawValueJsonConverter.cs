using System.Text.Json;
using System.Text.Json.Serialization;

namespace eQuantic.Linq.Expressions.Serialization;

/// <summary>
/// Writes constant values using their runtime type and reads them back as raw <see cref="JsonElement"/>s,
/// which are materialized later against the constant's <see cref="Metadata.TypeRef"/>.
/// </summary>
public sealed class RawValueJsonConverter : JsonConverter<object?>
{
    /// <inheritdoc />
    public override bool HandleNull => true;

    /// <inheritdoc />
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        return JsonElement.ParseValue(ref reader);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case JsonElement element:
                element.WriteTo(writer);
                break;
            default:
                JsonSerializer.Serialize(writer, value, value.GetType(), options);
                break;
        }
    }
}
