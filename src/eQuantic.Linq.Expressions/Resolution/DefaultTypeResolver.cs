using System.Collections.Concurrent;
using eQuantic.Linq.Expressions.Metadata;

namespace eQuantic.Linq.Expressions.Resolution;

/// <summary>
/// Default bidirectional type resolver. Emits portable references (aliases for well-known types,
/// no assembly name for core-library types, structural shapes for anonymous types) and resolves them
/// back by probing <see cref="Type.GetType(string)"/> and every loaded assembly, honoring the security
/// policy configured through <see cref="TypeResolutionOptions"/>.
/// </summary>
public sealed class DefaultTypeResolver : ITypeResolver
{
    private readonly TypeResolutionOptions _options;
    private readonly ConcurrentDictionary<string, Type> _resolveCache = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<Type, TypeRef> _typeRefCache = new();

    /// <summary>Creates a resolver with a permissive default policy.</summary>
    public DefaultTypeResolver()
        : this(new TypeResolutionOptions())
    {
    }

    /// <summary>Creates a resolver with an explicit policy.</summary>
    /// <param name="options">Naming and security policy.</param>
    public DefaultTypeResolver(TypeResolutionOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public TypeRef GetTypeRef(Type type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        // Cached instances are shared; TypeRef is treated as immutable once built.
        return _typeRefCache.TryGetValue(type, out var cached) ? cached : _typeRefCache.GetOrAdd(type, BuildTypeRef(type));
    }

    private TypeRef BuildTypeRef(Type type)
    {
        if (type.IsGenericParameter)
        {
            throw new ExpressionSerializationException(
                $"Open generic parameter '{type.Name}' cannot be serialized; expression trees only contain closed types.");
        }

        if (type.IsPointer)
        {
            throw new ExpressionSerializationException($"Pointer type '{type}' is not supported.");
        }

        if (type.IsByRef)
        {
            return new TypeRef { IsByRef = true, ElementType = GetTypeRef(type.GetElementType()!) };
        }

        if (type.IsArray)
        {
            return new TypeRef { ArrayRank = type.GetArrayRank(), ElementType = GetTypeRef(type.GetElementType()!) };
        }

        if (AnonymousTypeFactory.IsAnonymous(type) || AnonymousTypeFactory.IsGenerated(type))
        {
            return new TypeRef
            {
                IsAnonymous = true,
                Properties = GetAnonymousShape(type),
            };
        }

        var knownAlias = FindKnownAlias(type);
        if (knownAlias is not null)
        {
            return new TypeRef(knownAlias);
        }

        if (TypeAliases.TryGetAlias(type, out var alias))
        {
            return new TypeRef(alias);
        }

        if (type.IsConstructedGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            return new TypeRef(definition.FullName!, AssemblyNameOf(definition))
            {
                GenericArguments = type.GetGenericArguments().Select(GetTypeRef).ToList(),
            };
        }

        var fullName = type.FullName
                       ?? throw new ExpressionSerializationException($"Type '{type}' has no resolvable full name.");
        return new TypeRef(fullName, AssemblyNameOf(type));
    }

    /// <inheritdoc />
    public Type ResolveType(TypeRef typeRef)
    {
        if (typeRef is null)
        {
            throw new ArgumentNullException(nameof(typeRef));
        }

        if (typeRef.IsAnonymous)
        {
            var shape = (typeRef.Properties ?? [])
                .Select(p => new KeyValuePair<string, Type>(p.Name, ResolveType(p.Type)))
                .ToList();
            return AnonymousTypeFactory.Shared.GetOrCreate(shape);
        }

        if (typeRef.IsByRef)
        {
            return ResolveType(RequireElement(typeRef)).MakeByRefType();
        }

        if (typeRef.ArrayRank > 0)
        {
            var element = ResolveType(RequireElement(typeRef));
            return typeRef.ArrayRank == 1 ? element.MakeArrayType() : element.MakeArrayType(typeRef.ArrayRank);
        }

        if (string.IsNullOrEmpty(typeRef.Name))
        {
            throw new TypeResolutionException("Type reference has no name.");
        }

        var resolved = ResolveNamed(typeRef.Name!, typeRef.Assembly);

        if (typeRef.GenericArguments is { Count: > 0 })
        {
            var arguments = typeRef.GenericArguments.Select(ResolveType).ToArray();
            try
            {
                return resolved.MakeGenericType(arguments);
            }
            catch (ArgumentException exception)
            {
                throw new TypeResolutionException($"Cannot construct generic type '{typeRef}'.", exception);
            }
        }

        return resolved;
    }

    private static TypeRef RequireElement(TypeRef typeRef) =>
        typeRef.ElementType ?? throw new TypeResolutionException($"Type reference '{typeRef}' is missing its element type.");

    private List<AnonymousTypeProperty> GetAnonymousShape(Type type)
    {
        var properties = type.GetProperties();

        // Anonymous types expose one constructor whose parameters follow the declaration order;
        // use it as the canonical ordering of the shape.
        var constructors = type.GetConstructors();
        if (constructors.Length == 1)
        {
            var order = constructors[0].GetParameters()
                .Select((parameter, index) => new { parameter.Name, index })
                .ToDictionary(x => x.Name ?? string.Empty, x => x.index, StringComparer.OrdinalIgnoreCase);

            if (order.Count == properties.Length)
            {
                properties = properties
                    .OrderBy(p => order.TryGetValue(p.Name, out var index) ? index : int.MaxValue)
                    .ToArray();
            }
        }

        return properties
            .Select(p => new AnonymousTypeProperty(p.Name, GetTypeRef(p.PropertyType)))
            .ToList();
    }

    private string? FindKnownAlias(Type type)
    {
        if (_options.KnownTypes.Count == 0)
        {
            return null;
        }

        foreach (var pair in _options.KnownTypes)
        {
            if (pair.Value == type)
            {
                return pair.Key;
            }
        }

        return null;
    }

    private static string? AssemblyNameOf(Type type) =>
        type.Assembly == typeof(object).Assembly ? null : type.Assembly.GetName().Name;

    private Type ResolveNamed(string name, string? assembly)
    {
        if (TypeAliases.TryGetType(name, out var aliased))
        {
            return aliased;
        }

        if (_options.KnownTypes.TryGetValue(name, out var known))
        {
            return known;
        }

        var cacheKey = assembly is null ? name : $"{name}, {assembly}";
        if (_resolveCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var resolved = ProbeType(name, assembly)
                       ?? throw new TypeResolutionException(
                           $"Type '{name}'{(assembly is null ? string.Empty : $" (assembly '{assembly}')")} could not be resolved.");

        EnforcePolicy(resolved, name);
        _resolveCache[cacheKey] = resolved;
        return resolved;
    }

    private static Type? ProbeType(string name, string? assembly)
    {
        var direct = Type.GetType(name, throwOnError: false);
        if (direct is not null)
        {
            return direct;
        }

        if (assembly is not null)
        {
            var qualified = Type.GetType($"{name}, {assembly}", throwOnError: false);
            if (qualified is not null)
            {
                return qualified;
            }
        }

        var loaded = AppDomain.CurrentDomain.GetAssemblies();

        if (assembly is not null)
        {
            foreach (var candidate in loaded)
            {
                if (string.Equals(candidate.GetName().Name, assembly, StringComparison.OrdinalIgnoreCase))
                {
                    var found = candidate.GetType(name, throwOnError: false);
                    if (found is not null)
                    {
                        return found;
                    }
                }
            }
        }

        foreach (var candidate in loaded)
        {
            var found = candidate.GetType(name, throwOnError: false);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>
    /// Structural core types that stay resolvable under <see cref="TypeResolutionOptions.Strict"/>:
    /// they are required to represent lambdas and LINQ pipelines and expose no dangerous surface by themselves.
    /// </summary>
    private static readonly HashSet<string> SafeCoreNames = new(StringComparer.Ordinal)
    {
        "System.Nullable`1",
        "System.Linq.Enumerable",
        "System.Linq.Queryable",
        "System.Linq.IQueryable`1",
        "System.Linq.IOrderedQueryable`1",
        "System.Linq.IGrouping`2",
        "System.Linq.Expressions.Expression`1",
        "System.Collections.Generic.IEnumerable`1",
        "System.Collections.Generic.IOrderedEnumerable`1",
        "System.Collections.Generic.List`1",
        "System.Collections.Generic.IList`1",
        "System.Collections.Generic.IReadOnlyList`1",
        "System.Collections.Generic.ICollection`1",
        "System.Collections.Generic.Dictionary`2",
        "System.Collections.Generic.IDictionary`2",
        "System.Collections.Generic.KeyValuePair`2",
        "System.Collections.Generic.HashSet`1",
    };

    private static bool IsSafeCoreName(string name) =>
        SafeCoreNames.Contains(name)
        || name.StartsWith("System.Func`", StringComparison.Ordinal)
        || name.StartsWith("System.Action`", StringComparison.Ordinal)
        || name == "System.Action"
        || name.StartsWith("System.ValueTuple`", StringComparison.Ordinal)
        || name.StartsWith("System.Tuple`", StringComparison.Ordinal);

    private void EnforcePolicy(Type resolved, string requestedName)
    {
        if (!_options.Strict)
        {
            return;
        }

        if (IsSafeCoreName(requestedName))
        {
            return;
        }

        var assemblyName = resolved.Assembly.GetName().Name;
        if (assemblyName is not null && _options.AllowedAssemblies.Contains(assemblyName))
        {
            return;
        }

        var ns = resolved.Namespace ?? string.Empty;
        foreach (var allowed in _options.AllowedNamespaces)
        {
            if (ns.StartsWith(allowed, StringComparison.Ordinal))
            {
                return;
            }
        }

        throw new TypeResolutionException(
            $"Type '{requestedName}' resolved to '{resolved.FullName}' but is not allowed by the strict resolution policy. " +
            "Register it via TypeResolutionOptions.RegisterType or allow its assembly/namespace.");
    }
}
