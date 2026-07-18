namespace eQuantic.Linq.Expressions.Resolution;

/// <summary>
/// Security and naming policy for <see cref="DefaultTypeResolver"/>. Deserializing type names is a classic
/// attack surface; enable <see cref="Strict"/> and register the contracts you expect when consuming
/// payloads from untrusted sources.
/// </summary>
public sealed class TypeResolutionOptions
{
    /// <summary>
    /// When enabled, only well-known aliases, <see cref="KnownTypes"/> and types from
    /// <see cref="AllowedAssemblies"/>/<see cref="AllowedNamespaces"/> can be resolved.
    /// </summary>
    public bool Strict { get; set; }

    /// <summary>Explicitly registered types, keyed by the name emitted/accepted in the payload.</summary>
    public IDictionary<string, Type> KnownTypes { get; } = new Dictionary<string, Type>(StringComparer.Ordinal);

    /// <summary>Simple assembly names whose types may be resolved when <see cref="Strict"/> is enabled.</summary>
    public ISet<string> AllowedAssemblies { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Namespace prefixes whose types may be resolved when <see cref="Strict"/> is enabled.</summary>
    public IList<string> AllowedNamespaces { get; } = new List<string>();

    /// <summary>Registers a type, optionally under a custom payload alias (defaults to its full name).</summary>
    /// <param name="type">Type to register.</param>
    /// <param name="alias">Custom name used in payloads; defaults to <see cref="Type.FullName"/>.</param>
    public TypeResolutionOptions RegisterType(Type type, string? alias = null)
    {
        KnownTypes[alias ?? type.FullName ?? type.Name] = type;
        return this;
    }

    /// <summary>Registers <typeparamref name="T"/>, optionally under a custom payload alias.</summary>
    /// <typeparam name="T">Type to register.</typeparam>
    /// <param name="alias">Custom name used in payloads; defaults to the full name.</param>
    public TypeResolutionOptions RegisterType<T>(string? alias = null) => RegisterType(typeof(T), alias);
}
