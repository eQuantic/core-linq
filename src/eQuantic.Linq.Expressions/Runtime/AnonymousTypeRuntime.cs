using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace eQuantic.Linq.Expressions.Runtime;

/// <summary>
/// Support helpers invoked by runtime-emitted anonymous types. Public only because emitted IL must
/// be able to call into it; not intended for direct use. Property access goes through compiled
/// accessors (no per-call reflection).
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class AnonymousTypeRuntime
{
    private static readonly ConcurrentDictionary<Type, (string Name, Func<object, object?> Get)[]> AccessorsCache = new();

    private static (string Name, Func<object, object?> Get)[] AccessorsOf(Type type) =>
        AccessorsCache.GetOrAdd(type, static t =>
        {
            var properties = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var accessors = new (string, Func<object, object?>)[properties.Length];

            for (var i = 0; i < properties.Length; i++)
            {
                var instance = Expression.Parameter(typeof(object), "instance");
                var body = Expression.Convert(
                    Expression.Property(Expression.Convert(instance, t), properties[i]),
                    typeof(object));
                accessors[i] = (properties[i].Name, Expression.Lambda<Func<object, object?>>(body, instance).Compile());
            }

            return accessors;
        });

    /// <summary>Structural equality over public properties, mirroring compiler-generated anonymous type semantics.</summary>
    public static bool ObjectsEqual(object? left, object? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null || left.GetType() != right.GetType())
        {
            return false;
        }

        foreach (var (_, get) in AccessorsOf(left.GetType()))
        {
            if (!Equals(get(left), get(right)))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Structural hash code over public properties.</summary>
    public static int HashOf(object instance)
    {
        unchecked
        {
            var hash = 17;
            foreach (var (_, get) in AccessorsOf(instance.GetType()))
            {
                var value = get(instance);
                hash = (hash * 31) + (value?.GetHashCode() ?? 0);
            }

            return hash;
        }
    }

    /// <summary>Renders the instance using anonymous-type formatting: <c>{ Name = value, … }</c> (culture-invariant for determinism).</summary>
    public static string Render(object instance)
    {
        var builder = new StringBuilder("{ ");
        var first = true;
        foreach (var (name, get) in AccessorsOf(instance.GetType()))
        {
            if (!first)
            {
                builder.Append(", ");
            }

            var value = get(instance);
            builder.Append(name).Append(" = ").Append(
                value is IFormattable formattable
                    ? formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture)
                    : value);
            first = false;
        }

        builder.Append(" }");
        return builder.ToString();
    }
}
