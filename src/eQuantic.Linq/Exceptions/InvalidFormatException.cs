using System.Runtime.Serialization;

namespace eQuantic.Linq.Exceptions;

/// <summary>
/// Exception to detail what happens when Filtering or Sorting fail to receive valid parameters meaning they are invalid according to the expected regex
/// </summary>
/// <seealso cref="System.Exception" />
[Serializable]
public class InvalidFormatException : FormatException
{
    public InvalidFormatException(string expressionFor, string expectedFormat, string? simplifiedFormat = null) : base(BuildMessage(expressionFor, expectedFormat, simplifiedFormat))
    {
    }

    // Without this constructor, deserialization will fail
    protected InvalidFormatException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    private static string BuildMessage(string expressionFor, string expectedFormat, string? simplifiedFormat) =>
        string.IsNullOrEmpty(simplifiedFormat) 
            ? $"Invalid expression for: {expressionFor}. Expected something in the following format: \"{expectedFormat}\"."
            : $"Invalid expression for: {expressionFor}. Expected something in the following format: \"{expectedFormat}\". Alternatively you can use the simplified version for the equality operator: \"{simplifiedFormat}\".";
}