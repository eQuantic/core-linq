namespace eQuantic.Linq.Web;

/// <summary>Raised when a query-string expression has invalid syntax.</summary>
public class QueryStringParseException : FormatException
{
    /// <summary>Creates the exception.</summary>
    /// <param name="message">Description of the syntax problem.</param>
    /// <param name="position">Zero-based character position where the problem was detected.</param>
    /// <param name="text">The text being parsed.</param>
    public QueryStringParseException(string message, int position, string text)
        : base($"{message} (at position {position} in \"{text}\")")
    {
        Position = position;
        Text = text;
    }

    /// <summary>Zero-based character position where the problem was detected.</summary>
    public int Position { get; }

    /// <summary>The text being parsed.</summary>
    public string Text { get; }
}
