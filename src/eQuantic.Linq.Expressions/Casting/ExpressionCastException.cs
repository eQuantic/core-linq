namespace eQuantic.Linq.Expressions.Casting;

/// <summary>Raised when an expression cannot be cast between the source (DTO) and target (entity) shapes.</summary>
public class ExpressionCastException : Exception
{
    /// <summary>Creates the exception with a message.</summary>
    public ExpressionCastException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and inner exception.</summary>
    public ExpressionCastException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
