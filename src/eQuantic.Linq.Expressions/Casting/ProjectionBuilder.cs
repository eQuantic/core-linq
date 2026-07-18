using System.Linq.Expressions;
using eQuantic.Linq.Expressions.Resolution;

namespace eQuantic.Linq.Expressions.Casting;

/// <summary>Builds entity → DTO materializers (the reverse direction of the cast).</summary>
internal static class ProjectionBuilder
{
    public static LambdaExpression Build(Type source, Type target, CastRegistry registry)
    {
        var parameter = Expression.Parameter(target, "e");
        var body = BuildInit(source, target, parameter, registry);
        return Expression.Lambda(body, parameter);
    }

    private static Expression BuildInit(Type source, Type target, Expression targetInstance, CastRegistry registry)
    {
        var pair = registry.Find(source, target);
        var bindings = new List<MemberBinding>();

        foreach (var property in source.GetProperties().Where(p => p.CanWrite))
        {
            Expression? value = null;

            if (pair is not null && pair.TryGetMap(property.Name, out var map))
            {
                value = ParameterReplacer.Replace(map.Body, map.Parameters[0], targetInstance);
            }
            else if (pair?.AutoMapByName != false)
            {
                var names = new List<string> { property.Name };
                if (pair?.ColumnFallback != false && MemberResolver.GetColumnName(property) is { } column)
                {
                    names.Add(column);
                }

                foreach (var name in names)
                {
                    var found = MemberResolver.FindOnType(target, name, kind: null);
                    if (found is null)
                    {
                        continue;
                    }

                    var access = Expression.MakeMemberAccess(targetInstance, found);

                    if (property.PropertyType.IsAssignableFrom(access.Type))
                    {
                        value = access;
                    }
                    else if (ListElement(property.PropertyType) is { } sourceElement
                             && EnumerableElement(access.Type) is { } targetElement
                             && registry.Find(sourceElement, targetElement) is not null)
                    {
                        // registered nested pair over collections: e.Items.Select(item => new ItemDto {…}).ToList()
                        var nested = Build(sourceElement, targetElement, registry);
                        var select = Expression.Call(
                            typeof(Enumerable), nameof(Enumerable.Select), [targetElement, sourceElement], access, nested);
                        value = Expression.Call(typeof(Enumerable), nameof(Enumerable.ToList), [sourceElement], select);
                    }
                    else if (registry.Find(property.PropertyType, access.Type) is not null)
                    {
                        value = BuildInit(property.PropertyType, access.Type, access, registry);
                    }

                    if (value is not null)
                    {
                        break;
                    }
                }
            }

            if (value is not null)
            {
                bindings.Add(Expression.Bind(property, value));
            }
        }

        return Expression.MemberInit(Expression.New(source), bindings);
    }

    private static Type? ListElement(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) ? type.GetGenericArguments()[0] : null;

    private static Type? EnumerableElement(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return type.GetGenericArguments()[0];
        }

        foreach (var contract in type.GetInterfaces())
        {
            if (contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return contract.GetGenericArguments()[0];
            }
        }

        return null;
    }
}
