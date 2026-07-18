using System.Text;
using System.Text.Json.Serialization;

namespace eQuantic.Linq.Expressions.Metadata;

/// <summary>
/// Portable, structured reference to a .NET <see cref="Type"/>. Designed to survive
/// cross-runtime transport: well-known types use short aliases (<c>int</c>, <c>string</c>, <c>guid</c>…),
/// core-library types omit the assembly name, generics/arrays/by-ref types are represented structurally
/// and anonymous types are described by their property shape so they can be re-materialized anywhere.
/// </summary>
public sealed class TypeRef
{
    /// <summary>Full type name (namespace + name, <c>+</c> for nested types) or a well-known alias. For generic types this is the open definition name (e.g. <c>System.Collections.Generic.List`1</c>). Null for arrays, by-ref and anonymous types.</summary>
    public string? Name { get; set; }

    /// <summary>Simple assembly name. Omitted for core-library types so the reference stays portable across runtimes.</summary>
    public string? Assembly { get; set; }

    /// <summary>Closed generic arguments, when the referenced type is a constructed generic.</summary>
    public List<TypeRef>? GenericArguments { get; set; }

    /// <summary>Element type for arrays and by-ref types.</summary>
    public TypeRef? ElementType { get; set; }

    /// <summary>Array rank. <c>1</c> is a vector (<c>T[]</c>), greater values are multi-dimensional arrays. <c>0</c> when the type is not an array.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int ArrayRank { get; set; }

    /// <summary>Whether the reference is a by-ref type (<c>T&amp;</c>); <see cref="ElementType"/> holds the referenced type.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsByRef { get; set; }

    /// <summary>Whether this reference describes an anonymous (compiler-generated projection) type by shape.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsAnonymous { get; set; }

    /// <summary>Ordered property shape of an anonymous type.</summary>
    public List<AnonymousTypeProperty>? Properties { get; set; }

    /// <summary>Creates an empty reference; fill properties via object initializer.</summary>
    public TypeRef()
    {
    }

    /// <summary>Creates a named reference.</summary>
    /// <param name="name">Full type name or alias.</param>
    /// <param name="assembly">Optional simple assembly name.</param>
    public TypeRef(string name, string? assembly = null)
    {
        Name = name;
        Assembly = assembly;
    }

    /// <summary>Human-friendly rendering used by diagnostics and exception messages.</summary>
    public override string ToString()
    {
        if (IsAnonymous)
        {
            var props = Properties is null ? string.Empty : string.Join(", ", Properties.Select(p => $"{p.Name}: {p.Type}"));
            return $"anonymous {{ {props} }}";
        }

        if (IsByRef)
        {
            return $"{ElementType}&";
        }

        if (ArrayRank > 0)
        {
            var commas = new string(',', ArrayRank - 1);
            return $"{ElementType}[{commas}]";
        }

        if (GenericArguments is { Count: > 0 })
        {
            var builder = new StringBuilder();
            var name = Name ?? "?";
            var tick = name.IndexOf('`');
            builder.Append(tick >= 0 ? name.Substring(0, tick) : name);
            builder.Append('<');
            builder.Append(string.Join(", ", GenericArguments.Select(a => a.ToString())));
            builder.Append('>');
            return builder.ToString();
        }

        return Name ?? "?";
    }
}

/// <summary>Name/type pair describing one property of an anonymous type shape.</summary>
public sealed class AnonymousTypeProperty
{
    /// <summary>Property name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Property type.</summary>
    public TypeRef Type { get; set; } = new();

    /// <summary>Creates an empty property definition.</summary>
    public AnonymousTypeProperty()
    {
    }

    /// <summary>Creates a property definition.</summary>
    /// <param name="name">Property name.</param>
    /// <param name="type">Property type reference.</param>
    public AnonymousTypeProperty(string name, TypeRef type)
    {
        Name = name;
        Type = type;
    }
}
