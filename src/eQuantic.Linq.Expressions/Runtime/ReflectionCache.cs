using System.Collections.Concurrent;
using System.Reflection;

namespace eQuantic.Linq.Expressions.Runtime;

/// <summary>
/// Process-wide reflection caches. Reflection lookups (method tables, member searches, resolved
/// signatures) are performed once per shape and then served from memory, keeping serialization and
/// reconstruction hot paths allocation-light.
/// </summary>
internal static class ReflectionCache
{
    private const BindingFlags AllMembers =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    private static readonly ConcurrentDictionary<Type, Dictionary<string, MethodInfo[]>> MethodsByType = new();
    private static readonly ConcurrentDictionary<string, MethodInfo> ResolvedMethods = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<(Type Type, string Name, object? Kind), MemberInfo?> FoundMembers = new();

    /// <summary>All methods named <paramref name="name"/> on <paramref name="type"/> (public and non-public, instance and static).</summary>
    public static MethodInfo[] MethodsNamed(Type type, string name)
    {
        var byName = MethodsByType.GetOrAdd(type, static t =>
        {
            var map = new Dictionary<string, MethodInfo[]>(StringComparer.Ordinal);
            foreach (var group in t.GetMethods(AllMembers).GroupBy(m => m.Name, StringComparer.Ordinal))
            {
                // Deterministic candidate order across runtimes: non-generic before generic,
                // then by stable signature text — ambiguous lean payloads always bind the same way.
                map[group.Key] = group
                    .OrderBy(m => m.IsGenericMethodDefinition ? 1 : 0)
                    .ThenBy(m => m.GetParameters().Length)
                    .ThenBy(m => m.ToString(), StringComparer.Ordinal)
                    .ToArray();
            }

            return map;
        });

        if (byName.TryGetValue(name, out var methods))
        {
            return methods;
        }

        // Hand-written payloads (front-ends) often use camelCase method names.
        foreach (var pair in byName)
        {
            if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                return pair.Value;
            }
        }

        return [];
    }

    /// <summary>Caches a fully resolved method under a structural signature key.</summary>
    public static MethodInfo GetOrResolveMethod(string signatureKey, Func<MethodInfo?> resolve)
    {
        if (ResolvedMethods.TryGetValue(signatureKey, out var cached))
        {
            return cached;
        }

        var resolved = resolve();
        if (resolved is null)
        {
            throw new TypeResolutionException($"Method not found for signature '{signatureKey}'.");
        }

        ResolvedMethods[signatureKey] = resolved;
        return resolved;
    }

    /// <summary>Caches member-by-name lookups (no indexer parameters).</summary>
    public static MemberInfo? GetOrFindMember(Type type, string name, object? kind, Func<MemberInfo?> find)
    {
        return FoundMembers.GetOrAdd((type, name, kind), _ => find());
    }
}
