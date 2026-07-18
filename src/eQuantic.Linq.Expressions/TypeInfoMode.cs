namespace eQuantic.Linq.Expressions;

/// <summary>Controls how much type information is embedded in serialized payloads.</summary>
public enum TypeInfoMode
{
    /// <summary>
    /// Every node carries complete type/member references. Payloads are self-contained and can be
    /// rebuilt without any external knowledge. This is the mode used by <see cref="ExpressionSerializer.ToNode"/>.
    /// </summary>
    Full,

    /// <summary>
    /// Type information that can be re-inferred from the model's root type is omitted: parameter types,
    /// member declaring types, constant types recoverable from context (binary sibling, method parameter,
    /// assigned member) and canonical delegate types. Produces lean, hand-writable payloads anchored on a
    /// root entity (see <see cref="ExpressionModel{TRoot}"/>).
    /// </summary>
    Minimal,
}
