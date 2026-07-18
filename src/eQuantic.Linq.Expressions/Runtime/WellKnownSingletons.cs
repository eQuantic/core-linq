using System.Reflection;

namespace eQuantic.Linq.Expressions.Runtime;

/// <summary>
/// Maps well-known singleton instances (comparers) back to the static member that exposes them, so
/// constants holding e.g. <see cref="StringComparer.OrdinalIgnoreCase"/> serialize as a portable
/// static member access instead of an unserializable object value.
/// </summary>
internal static class WellKnownSingletons
{
    private static readonly (object Instance, PropertyInfo Property)[] Fixed = BuildFixed();

    private static (object, PropertyInfo)[] BuildFixed()
    {
        // Only stable, reference-cached singletons qualify (culture-sensitive comparers are
        // recreated per access and cannot be matched by reference).
        string[] names =
        [
            nameof(StringComparer.Ordinal),
            nameof(StringComparer.OrdinalIgnoreCase),
            nameof(StringComparer.InvariantCulture),
            nameof(StringComparer.InvariantCultureIgnoreCase),
        ];

        var list = new List<(object, PropertyInfo)>(names.Length);
        foreach (var name in names)
        {
            var property = typeof(StringComparer).GetProperty(name, BindingFlags.Public | BindingFlags.Static);
            var value = property?.GetValue(null);
            if (property is not null && value is not null)
            {
                list.Add((value, property));
            }
        }

        return list.ToArray();
    }

    /// <summary>Tries to map a value to the static property exposing it (e.g. <c>EqualityComparer&lt;T&gt;.Default</c>).</summary>
    public static bool TryGetMember(object value, out MemberInfo member)
    {
        foreach (var (instance, property) in Fixed)
        {
            if (ReferenceEquals(instance, value))
            {
                member = property;
                return true;
            }
        }

        for (var current = value.GetType(); current is not null; current = current.BaseType)
        {
            if (!current.IsGenericType)
            {
                continue;
            }

            var definition = current.GetGenericTypeDefinition();
            if (definition != typeof(EqualityComparer<>) && definition != typeof(Comparer<>))
            {
                continue;
            }

            var defaultProperty = current.GetProperty("Default", BindingFlags.Public | BindingFlags.Static);
            if (defaultProperty is not null && ReferenceEquals(defaultProperty.GetValue(null), value))
            {
                member = defaultProperty;
                return true;
            }
        }

        member = null!;
        return false;
    }
}
