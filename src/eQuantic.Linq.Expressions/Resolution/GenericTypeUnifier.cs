using System.Linq.Expressions;

namespace eQuantic.Linq.Expressions.Resolution;

/// <summary>
/// Lightweight generic type unification used by call-site inference: matches open method parameter
/// types against actual argument types, binding generic method parameters along the way
/// (a pragmatic subset of C# type inference sufficient for LINQ shapes).
/// </summary>
internal static class GenericTypeUnifier
{
    /// <summary>Substitutes bound generic parameters in <paramref name="type"/>; <paramref name="closed"/> reports whether the result contains no unbound parameters.</summary>
    public static Type Substitute(Type type, IReadOnlyDictionary<Type, Type> bindings, out bool closed)
    {
        if (type.IsGenericParameter)
        {
            if (bindings.TryGetValue(type, out var bound))
            {
                closed = true;
                return bound;
            }

            closed = false;
            return type;
        }

        if (!type.ContainsGenericParameters)
        {
            closed = true;
            return type;
        }

        if (type.IsArray)
        {
            var element = Substitute(type.GetElementType()!, bindings, out closed);
            return closed ? (type.GetArrayRank() == 1 ? element.MakeArrayType() : element.MakeArrayType(type.GetArrayRank())) : type;
        }

        if (type.IsByRef)
        {
            var element = Substitute(type.GetElementType()!, bindings, out closed);
            return closed ? element.MakeByRefType() : type;
        }

        if (type.IsGenericType)
        {
            var arguments = type.GetGenericArguments();
            var substituted = new Type[arguments.Length];
            closed = true;

            for (var i = 0; i < arguments.Length; i++)
            {
                substituted[i] = Substitute(arguments[i], bindings, out var argumentClosed);
                closed &= argumentClosed;
            }

            return closed ? type.GetGenericTypeDefinition().MakeGenericType(substituted) : type;
        }

        closed = false;
        return type;
    }

    /// <summary>
    /// Unifies an open parameter type with an actual argument type, accumulating generic bindings.
    /// For closed parameter types this degrades to an assignability check.
    /// </summary>
    public static bool Unify(Type open, Type actual, Dictionary<Type, Type> bindings)
    {
        if (!open.ContainsGenericParameters)
        {
            return open.IsAssignableFrom(actual) || QuoteCompatible(open, actual);
        }

        if (open.IsGenericParameter)
        {
            if (bindings.TryGetValue(open, out var existing))
            {
                return existing == actual || existing.IsAssignableFrom(actual);
            }

            bindings[open] = actual;
            return true;
        }

        if (open.IsArray)
        {
            return actual.IsArray
                   && open.GetArrayRank() == actual.GetArrayRank()
                   && Unify(open.GetElementType()!, actual.GetElementType()!, bindings);
        }

        if (open.IsByRef)
        {
            return actual.IsByRef && Unify(open.GetElementType()!, actual.GetElementType()!, bindings);
        }

        if (open.IsGenericType)
        {
            var definition = open.GetGenericTypeDefinition();

            // Expression<TDelegate> parameters accept plain lambdas (Expression.Call quotes them).
            if (definition == typeof(Expression<>) && typeof(Delegate).IsAssignableFrom(StripOpenness(actual)))
            {
                return Unify(open.GetGenericArguments()[0], actual, bindings);
            }

            foreach (var candidate in SelfAndImplementations(actual))
            {
                if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() == definition)
                {
                    var openArguments = open.GetGenericArguments();
                    var actualArguments = candidate.GetGenericArguments();

                    for (var i = 0; i < openArguments.Length; i++)
                    {
                        if (!Unify(openArguments[i], actualArguments[i], bindings))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }
        }

        return false;
    }

    private static Type StripOpenness(Type type) => type;

    private static bool QuoteCompatible(Type parameterType, Type actual)
    {
        // Expression<F> parameter, LambdaExpression argument whose delegate type is F.
        return parameterType.IsGenericType
               && parameterType.GetGenericTypeDefinition() == typeof(Expression<>)
               && parameterType.GetGenericArguments()[0].IsAssignableFrom(actual);
    }

    private static IEnumerable<Type> SelfAndImplementations(Type type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            yield return current;
        }

        foreach (var contract in type.GetInterfaces())
        {
            yield return contract;
        }
    }
}
