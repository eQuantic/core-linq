using System.Runtime.Serialization;

namespace eQuantic.Linq.Exceptions;

/// <summary>
/// Exception to detail what happens when Filtering or Sorting fail to receive valid parameters meaning they are invalid according to the expected regex
/// </summary>
/// <seealso cref="System.Exception" />
[Serializable]
public class InvalidFormatException : FormatException
{
    public InvalidFormatException(string expressionFor, string expectedFormat, string? simplifiedFormat = null) : base($"Invalid expression for: {expressionFor}. Expected something in the following format: \"{expectedFormat}\".{(!string.IsNullOrEmpty(simplifiedFormat) ? $"Alternatively you can use the simplified version for the equality operator: \"{simplifiedFormat}\"" : "")}")
    {
    }

    // Without this constructor, deserialization will fail
    protected InvalidFormatException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}