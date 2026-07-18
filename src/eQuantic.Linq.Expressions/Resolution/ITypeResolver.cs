using eQuantic.Linq.Expressions.Metadata;

namespace eQuantic.Linq.Expressions.Resolution;

/// <summary>
/// Converts runtime types to portable <see cref="TypeRef"/>s and back.
/// Implement to customize naming, apply contract mappings or harden type resolution.
/// </summary>
public interface ITypeResolver
{
    /// <summary>Builds a portable reference for a runtime type.</summary>
    /// <param name="type">Type to describe.</param>
    TypeRef GetTypeRef(Type type);

    /// <summary>Resolves a portable reference back to a runtime type.</summary>
    /// <param name="typeRef">Reference to resolve.</param>
    /// <exception cref="TypeResolutionException">The reference cannot be resolved (unknown type or blocked by policy).</exception>
    Type ResolveType(TypeRef typeRef);
}
