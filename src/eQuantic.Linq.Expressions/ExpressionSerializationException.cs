namespace eQuantic.Linq.Expressions;

/// <summary>Raised when an expression cannot be converted to or from the serializable model.</summary>
public class ExpressionSerializationException : Exception
{
    /// <summary>Creates the exception with a message.</summary>
    public ExpressionSerializationException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and inner exception.</summary>
    public ExpressionSerializationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>Raised when a <see cref="Metadata.TypeRef"/> or member reference cannot be resolved back to a runtime type/member.</summary>
public class TypeResolutionException : ExpressionSerializationException
{
    /// <summary>Creates the exception with a message.</summary>
    public TypeResolutionException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and inner exception.</summary>
    public TypeResolutionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
