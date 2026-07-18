#if !NETSTANDARD2_0
using System.Text.Json;
using System.Text.Json.Serialization;

namespace eQuantic.Linq.Expressions.Serialization;

/// <summary>Serializes <see cref="Index"/> using its literal form (<c>2</c> / <c>^1</c>).</summary>
internal sealed class IndexJsonConverter : JsonConverter<Index>
{
    public override Index Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        Parse(reader.GetString() ?? throw new JsonException("Index value expected."));

    public override void Write(Utf8JsonWriter writer, Index value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());

    internal static Index Parse(string text)
    {
        var fromEnd = text.StartsWith("^", StringComparison.Ordinal);
        var number = int.Parse(fromEnd ? text.Substring(1) : text, System.Globalization.CultureInfo.InvariantCulture);
        return new Index(number, fromEnd);
    }
}

/// <summary>Serializes <see cref="Range"/> using its literal form (<c>2..^1</c>).</summary>
internal sealed class RangeJsonConverter : JsonConverter<Range>
{
    public override Range Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString() ?? throw new JsonException("Range value expected.");
        var separator = text.IndexOf("..", StringComparison.Ordinal);
        if (separator < 0)
        {
            throw new JsonException($"Invalid range literal '{text}'.");
        }

        var startText = text.Substring(0, separator);
        var endText = text.Substring(separator + 2);

        var start = startText.Length == 0 ? Index.Start : IndexJsonConverter.Parse(startText);
        var end = endText.Length == 0 ? Index.End : IndexJsonConverter.Parse(endText);
        return new Range(start, end);
    }

    public override void Write(Utf8JsonWriter writer, Range value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
#endif
