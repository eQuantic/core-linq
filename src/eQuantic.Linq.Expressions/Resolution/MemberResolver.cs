using System.Reflection;
using System.Text;
using eQuantic.Linq.Expressions.Metadata;
using eQuantic.Linq.Expressions.Runtime;

namespace eQuantic.Linq.Expressions.Resolution;

/// <summary>Resolves serialized member references back to reflection members using closed-type matching.</summary>
internal static class MemberResolver
{
    internal const BindingFlags AllMembers =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    public static MethodInfo ResolveMethod(MethodRef reference, ITypeResolver resolver)
    {
        if (reference.DeclaringType is null || reference.ParameterTypes is null)
        {
            throw new TypeResolutionException(
                $"Method reference '{reference}' is incomplete; inferred method references can only be resolved with call-site context.");
        }

        var declaringType = resolver.ResolveType(reference.DeclaringType);
        var genericArguments = reference.GenericArguments?.Select(resolver.ResolveType).ToArray() ?? Type.EmptyTypes;
        var parameterTypes = reference.ParameterTypes.Select(resolver.ResolveType).ToArray();

        var key = BuildSignatureKey(declaringType, reference.Name, genericArguments, parameterTypes);
        return ReflectionCache.GetOrResolveMethod(
            key,
            () => FindMethod(declaringType, reference.Name, genericArguments, parameterTypes));
    }

    private static string BuildSignatureKey(Type declaringType, string name, Type[] genericArguments, Type[] parameterTypes)
    {
        var builder = new StringBuilder(declaringType.AssemblyQualifiedName ?? declaringType.ToString())
            .Append("::")
            .Append(name);

        foreach (var argument in genericArguments)
        {
            builder.Append("|g:").Append(argument.AssemblyQualifiedName ?? argument.ToString());
        }

        foreach (var parameter in parameterTypes)
        {
            builder.Append("|p:").Append(parameter.AssemblyQualifiedName ?? parameter.ToString());
        }

        return builder.ToString();
    }

    private static MethodInfo? FindMethod(Type declaringType, string name, Type[] genericArguments, Type[] parameterTypes)
    {
        foreach (var candidate in ReflectionCache.MethodsNamed(declaringType, name))
        {
            MethodInfo method;
            if (genericArguments.Length > 0)
            {
                if (!candidate.IsGenericMethodDefinition
                    || candidate.GetGenericArguments().Length != genericArguments.Length)
                {
                    continue;
                }

                try
                {
                    method = candidate.MakeGenericMethod(genericArguments);
                }
                catch (ArgumentException)
                {
                    // Generic constraints not satisfied by these arguments; try the next overload.
                    continue;
                }
            }
            else
            {
                if (candidate.IsGenericMethodDefinition)
                {
                    continue;
                }

                method = candidate;
            }

            if (ParametersMatch(method.GetParameters(), parameterTypes))
            {
                return method;
            }
        }

        return null;
    }

    public static ConstructorInfo ResolveConstructor(ConstructorRef reference, Type fallbackDeclaringType, ITypeResolver resolver)
    {
        var declaringType = reference.DeclaringType is null
            ? fallbackDeclaringType
            : resolver.ResolveType(reference.DeclaringType);

        if (reference.ParameterTypes is null)
        {
            throw new TypeResolutionException(
                $"Constructor reference '{reference}' is incomplete; inferred constructor references can only be resolved with call-site context.");
        }

        var parameterTypes = reference.ParameterTypes.Select(resolver.ResolveType).ToArray();

        foreach (var candidate in declaringType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (ParametersMatch(candidate.GetParameters(), parameterTypes))
            {
                return candidate;
            }
        }

        throw new TypeResolutionException($"Constructor not found: {reference}.");
    }

    public static MemberInfo ResolveMember(MemberRef reference, ITypeResolver resolver, Type? contextType = null)
    {
        var declaringType = reference.DeclaringType is null
            ? contextType ?? throw new TypeResolutionException(
                $"Member reference '{reference.Name}' has no declaring type and no context to infer it from.")
            : resolver.ResolveType(reference.DeclaringType);

        var indexParameterTypes = reference.ParameterTypes?.Select(resolver.ResolveType).ToArray();

        return FindOnType(declaringType, reference.Name, reference.Kind, indexParameterTypes)
               ?? throw new TypeResolutionException($"Member not found: '{reference.Name}' on '{declaringType}'.");
    }

    /// <summary>Finds a property or field by name walking the type hierarchy; probes properties first when the kind is unspecified.</summary>
    public static MemberInfo? FindOnType(Type type, string name, MemberKind? kind, Type[]? indexParameterTypes = null)
    {
        if (indexParameterTypes is null)
        {
            return ReflectionCache.GetOrFindMember(type, name, kind, () => FindOnTypeCore(type, name, kind, null));
        }

        return FindOnTypeCore(type, name, kind, indexParameterTypes);
    }

    private static MemberInfo? FindOnTypeCore(Type type, string name, MemberKind? kind, Type[]? indexParameterTypes)
    {
        // Exact-case first; camelCase payloads fall back to case-insensitive; finally, members whose
        // [Column("…")] attribute matches the requested name (column fallback).
        return FindOnTypeCore(type, name, kind, indexParameterTypes, StringComparison.Ordinal)
               ?? FindOnTypeCore(type, name, kind, indexParameterTypes, StringComparison.OrdinalIgnoreCase)
               ?? FindByColumnName(type, name, kind);
    }

    /// <summary>
    /// Reads the <c>[Column("…")]</c> name of a member, when present. Detection is attribute-name based
    /// (System.ComponentModel.DataAnnotations.Schema.ColumnAttribute) so no package reference is required.
    /// </summary>
    public static string? GetColumnName(MemberInfo member)
    {
        foreach (var attribute in member.GetCustomAttributes(inherit: true))
        {
            var type = attribute.GetType();
            if (type.FullName == "System.ComponentModel.DataAnnotations.Schema.ColumnAttribute")
            {
                return type.GetProperty("Name")?.GetValue(attribute) as string;
            }
        }

        return null;
    }

    private static MemberInfo? FindByColumnName(Type type, string name, MemberKind? kind)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (kind is null or MemberKind.Property)
            {
                foreach (var property in current.GetProperties(AllMembers | BindingFlags.DeclaredOnly))
                {
                    if (string.Equals(GetColumnName(property), name, StringComparison.OrdinalIgnoreCase))
                    {
                        return property;
                    }
                }
            }

            if (kind is null or MemberKind.Field)
            {
                foreach (var field in current.GetFields(AllMembers | BindingFlags.DeclaredOnly))
                {
                    if (string.Equals(GetColumnName(field), name, StringComparison.OrdinalIgnoreCase))
                    {
                        return field;
                    }
                }
            }
        }

        return null;
    }

    private static MemberInfo? FindOnTypeCore(Type type, string name, MemberKind? kind, Type[]? indexParameterTypes, StringComparison comparison)
    {
        if (kind is null or MemberKind.Property)
        {
            var property = FindProperty(type, name, indexParameterTypes, comparison);
            if (property is not null)
            {
                return property;
            }

            if (kind == MemberKind.Property)
            {
                return null;
            }
        }

        for (var current = type; current is not null; current = current.BaseType)
        {
            foreach (var field in current.GetFields(AllMembers | BindingFlags.DeclaredOnly))
            {
                if (string.Equals(field.Name, name, comparison))
                {
                    return field;
                }
            }
        }

        return null;
    }

    private static PropertyInfo? FindProperty(Type type, string name, Type[]? indexParameterTypes, StringComparison comparison)
    {
        PropertyInfo? fallback = null;

        for (var current = type; current is not null; current = current.BaseType)
        {
            foreach (var property in current.GetProperties(AllMembers | BindingFlags.DeclaredOnly))
            {
                if (!string.Equals(property.Name, name, comparison))
                {
                    continue;
                }

                if (indexParameterTypes is { Length: > 0 })
                {
                    if (!ParametersMatch(property.GetIndexParameters(), indexParameterTypes))
                    {
                        continue;
                    }
                }

                fallback ??= property;

                if (property.DeclaringType == current)
                {
                    return property;
                }
            }

            if (fallback is not null)
            {
                return fallback;
            }
        }

        // Interface members (e.g. accessing a property through an interface-typed expression).
        foreach (var contract in type.GetInterfaces())
        {
            foreach (var property in contract.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (string.Equals(property.Name, name, comparison))
                {
                    return property;
                }
            }
        }

        return fallback;
    }

    /// <summary>Reflection members are equal when they share module, token and declaring type — regardless of reflected type.</summary>
    public static bool SameMember(MemberInfo left, MemberInfo right) =>
        left.Equals(right)
        || (left.Module == right.Module
            && left.MetadataToken == right.MetadataToken
            && left.DeclaringType == right.DeclaringType);

    internal static bool ParametersMatch(ParameterInfo[] parameters, Type[] expected)
    {
        if (parameters.Length != expected.Length)
        {
            return false;
        }

        for (var i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].ParameterType != expected[i])
            {
                return false;
            }
        }

        return true;
    }
}
