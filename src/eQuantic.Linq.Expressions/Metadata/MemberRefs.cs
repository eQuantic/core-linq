namespace eQuantic.Linq.Expressions.Metadata;

/// <summary>Discriminates the kind of member referenced by a <see cref="MemberRef"/>.</summary>
public enum MemberKind
{
    /// <summary>A field.</summary>
    Field,

    /// <summary>A property (including indexers).</summary>
    Property,
}

/// <summary>
/// Serializable reference to a field or property. In inferred (lean) payloads only <see cref="Name"/>
/// is required for instance members: the declaring type is recovered from the target expression's type.
/// </summary>
public sealed class MemberRef
{
    /// <summary>Type that declares the member. Optional for instance members in inferred payloads.</summary>
    public TypeRef? DeclaringType { get; set; }

    /// <summary>Member name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Whether the member is a field or a property. Optional: when omitted, properties are probed first, then fields.</summary>
    public MemberKind? Kind { get; set; }

    /// <summary>Indexer parameter types, used to disambiguate overloaded indexers.</summary>
    public List<TypeRef>? ParameterTypes { get; set; }

    /// <inheritdoc />
    public override string ToString() => DeclaringType is null ? Name : $"{DeclaringType}.{Name}";
}

/// <summary>
/// Serializable reference to a method. Generic methods are stored closed: parameter types are the
/// substituted (closed) ones and <see cref="GenericArguments"/> hold the type arguments. In inferred
/// payloads, parameter types — and, for instance/extension calls, even the declaring type — may be
/// omitted and are recovered by overload/unification binding against the decoded arguments.
/// </summary>
public sealed class MethodRef
{
    /// <summary>Type that declares the method. Optional for instance calls in inferred payloads (extension methods are probed through <see cref="ExpressionSerializerOptions.ExtensionMethodTypes"/>).</summary>
    public TypeRef? DeclaringType { get; set; }

    /// <summary>Method name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Closed parameter types, in declaration order. Optional in inferred payloads.</summary>
    public List<TypeRef>? ParameterTypes { get; set; }

    /// <summary>Closed generic arguments when the method is generic. Optional in inferred payloads (recovered by unification).</summary>
    public List<TypeRef>? GenericArguments { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        var generics = GenericArguments is { Count: > 0 }
            ? $"<{string.Join(", ", GenericArguments.Select(g => g.ToString()))}>"
            : string.Empty;
        var parameters = ParameterTypes is null ? "…" : string.Join(", ", ParameterTypes.Select(p => p.ToString()));
        var owner = DeclaringType is null ? string.Empty : $"{DeclaringType}.";
        return $"{owner}{Name}{generics}({parameters})";
    }
}

/// <summary>Serializable reference to a constructor. The declaring type defaults to the enclosing <c>new</c> node's type.</summary>
public sealed class ConstructorRef
{
    /// <summary>Type that declares the constructor; defaults to the constructed type when omitted.</summary>
    public TypeRef? DeclaringType { get; set; }

    /// <summary>Parameter types, in declaration order. Optional in inferred payloads.</summary>
    public List<TypeRef>? ParameterTypes { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        var parameters = ParameterTypes is null ? "…" : string.Join(", ", ParameterTypes.Select(p => p.ToString()));
        return $"new {DeclaringType?.ToString() ?? "?"}({parameters})";
    }
}
