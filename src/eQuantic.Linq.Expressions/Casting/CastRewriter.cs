using System.Linq.Expressions;
using System.Reflection;
using eQuantic.Linq.Expressions.Resolution;
using eQuantic.Linq.Expressions.Runtime;

namespace eQuantic.Linq.Expressions.Casting;

/// <summary>
/// Rewrites expressions from a source shape (DTO) onto a target shape (entity): parameters are
/// retargeted, member accesses are resolved through explicit maps / name matching / column fallback,
/// generic method calls are re-bound by unification when element types change, and anonymous
/// projections are re-emitted with the rewritten argument types.
/// </summary>
internal sealed class CastRewriter : ExpressionVisitor
{
    private readonly CastRegistry _registry;
    private readonly Dictionary<ParameterExpression, ParameterExpression> _parameters = [];

    public CastRewriter(CastRegistry registry)
    {
        _registry = registry;
    }

    public LambdaExpression Rewrite(LambdaExpression lambda, Type sourceType, Type targetType)
    {
        if (lambda.Parameters.Count == 0 || lambda.Parameters[0].Type != sourceType)
        {
            throw new ExpressionCastException(
                $"The lambda's first parameter must be of type '{sourceType}' to cast it to '{targetType}'.");
        }

        var parameters = new ParameterExpression[lambda.Parameters.Count];
        parameters[0] = Expression.Parameter(targetType, lambda.Parameters[0].Name);
        _parameters[lambda.Parameters[0]] = parameters[0];

        for (var i = 1; i < lambda.Parameters.Count; i++)
        {
            parameters[i] = lambda.Parameters[i];
        }

        try
        {
            var body = Visit(lambda.Body)!;
            return Expression.Lambda(body, lambda.Name, lambda.TailCall, parameters);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            throw new ExpressionCastException(
                $"The expression could not be cast from '{sourceType.Name}' to '{targetType.Name}': {exception.Message} " +
                "A member mapping probably changes the value type — add an explicit CastOptions.Map(...) that preserves it.",
                exception);
        }
    }

    protected override Expression VisitParameter(ParameterExpression node) =>
        _parameters.TryGetValue(node, out var replaced) ? replaced : node;

    // ---------------------------------------------------------------- members

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression is null)
        {
            return base.VisitMember(node);
        }

        var target = Visit(node.Expression)!;

        // Untouched chain: the member still applies.
        if (target.Type == node.Expression.Type)
        {
            return node.Update(target);
        }

        var pair = _registry.Find(node.Expression.Type, target.Type);

        if (pair is not null && pair.TryGetMap(node.Member.Name, out var map))
        {
            var inlined = ParameterReplacer.Replace(map.Body, map.Parameters[0], target);

            if (inlined.Type != node.Type && !node.Type.IsAssignableFrom(inlined.Type))
            {
                throw new ExpressionCastException(
                    $"The map for '{node.Expression.Type.Name}.{node.Member.Name}' produces '{inlined.Type}', " +
                    $"which is not compatible with the member's type '{node.Type}'.");
            }

            return inlined;
        }

        if (pair?.AutoMapByName != false)
        {
            foreach (var candidateName in CandidateNames(node.Member, pair?.ColumnFallback != false))
            {
                var found = MemberResolver.FindOnType(target.Type, candidateName, kind: null);
                if (found is not null)
                {
                    return Expression.MakeMemberAccess(target, found);
                }
            }
        }

        throw new ExpressionCastException(
            $"Member '{node.Expression.Type.Name}.{node.Member.Name}' has no counterpart on '{target.Type.Name}'. " +
            "Add an explicit CastOptions.Map(...) for it.");
    }

    private static IEnumerable<string> CandidateNames(MemberInfo member, bool columnFallback)
    {
        yield return member.Name;

        if (columnFallback)
        {
            var columnName = MemberResolver.GetColumnName(member);
            if (columnName is not null && !string.Equals(columnName, member.Name, StringComparison.OrdinalIgnoreCase))
            {
                yield return columnName;
            }
        }
    }

    // ---------------------------------------------------------------- calls

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var instance = node.Object is null ? null : Visit(node.Object);

        var arguments = new Expression?[node.Arguments.Count];
        var deferred = new List<(int Index, LambdaExpression Lambda)>();
        var typesChanged = instance is not null && instance.Type != node.Object!.Type;
        var anythingChanged = instance != node.Object;

        for (var i = 0; i < node.Arguments.Count; i++)
        {
            var argument = node.Arguments[i];
            var lambda = argument as LambdaExpression
                         ?? (argument as UnaryExpression is { NodeType: ExpressionType.Quote } quote
                             ? quote.Operand as LambdaExpression
                             : null);

            if (lambda is not null)
            {
                deferred.Add((i, lambda));
                continue;
            }

            var visited = Visit(argument)!;
            arguments[i] = visited;
            typesChanged |= visited.Type != argument.Type;
            anythingChanged |= visited != argument;
        }

        // Nothing type-affecting happened: rewrite lambda bodies in place and rebuild.
        if (!typesChanged)
        {
            foreach (var (index, lambda) in deferred)
            {
                var body = Visit(lambda.Body)!;
                var rebuilt = body == lambda.Body ? lambda : Expression.Lambda(lambda.Type, body, lambda.Name, lambda.TailCall, lambda.Parameters);
                arguments[index] = rebuilt;
                anythingChanged |= rebuilt != lambda;
            }

            return anythingChanged
                ? RebuildCall(node.Method, instance, arguments!)
                : node;
        }

        return RebindCall(node, instance, arguments, deferred);
    }

    private Expression RebindCall(
        MethodCallExpression node,
        Expression? instance,
        Expression?[] arguments,
        List<(int Index, LambdaExpression Lambda)> deferred)
    {
        var method = node.Method;

        // Instance methods: re-resolve on the rewritten receiver type when needed.
        if (!method.IsStatic && instance is not null)
        {
            if (!method.DeclaringType!.IsAssignableFrom(instance.Type))
            {
                method = ResolveInstanceMethod(instance.Type, node, arguments, deferred);
            }

            foreach (var (index, lambda) in deferred)
            {
                arguments[index] = RetypeLambda(lambda, ExpectedParameterTypes(method.GetParameters()[index].ParameterType, lambda));
            }

            return Expression.Call(instance, method, arguments!);
        }

        if (!method.IsGenericMethod)
        {
            foreach (var (index, lambda) in deferred)
            {
                arguments[index] = RetypeLambda(lambda, ExpectedParameterTypes(method.GetParameters()[index].ParameterType, lambda));
            }

            return RebuildCall(method, instance, arguments!);
        }

        // Generic static (LINQ shape): re-infer generic arguments by unification over the rewritten arguments.
        var definition = method.GetGenericMethodDefinition();
        var parameters = definition.GetParameters();
        var bindings = new Dictionary<Type, Type>();

        for (var i = 0; i < arguments.Length; i++)
        {
            if (arguments[i] is { } known && !GenericTypeUnifier.Unify(parameters[i].ParameterType, known.Type, bindings))
            {
                throw RebindFailure(node, known.Type, i);
            }
        }

        var pending = new Queue<(int Index, LambdaExpression Lambda)>(deferred);
        var stagnation = 0;
        while (pending.Count > 0)
        {
            if (stagnation > pending.Count)
            {
                throw new ExpressionCastException(
                    $"Cannot re-infer the generic arguments of '{node.Method.Name}' after casting; add explicit maps for the involved members.");
            }

            var (index, lambda) = pending.Dequeue();
            var substituted = GenericTypeUnifier.Substitute(parameters[index].ParameterType, bindings, out _);
            var delegateShape = InferenceRulesUnwrap(substituted);
            var parameterTypes = DelegateParameterTypes(delegateShape, bindings, lambda.Parameters.Count, out var closed);

            if (!closed)
            {
                stagnation++;
                pending.Enqueue((index, lambda));
                continue;
            }

            var rebuilt = RetypeLambda(lambda, parameterTypes);
            arguments[index] = rebuilt;

            if (!GenericTypeUnifier.Unify(parameters[index].ParameterType, rebuilt.Type, bindings))
            {
                throw RebindFailure(node, rebuilt.Type, index);
            }

            stagnation = 0;
        }

        var open = definition.GetGenericArguments();
        var closedArguments = new Type[open.Length];
        for (var i = 0; i < open.Length; i++)
        {
            if (!bindings.TryGetValue(open[i], out var bound))
            {
                throw new ExpressionCastException(
                    $"Generic argument '{open[i].Name}' of '{node.Method.Name}' could not be re-inferred after casting.");
            }

            closedArguments[i] = bound;
        }

        MethodInfo closedMethod;
        try
        {
            closedMethod = definition.MakeGenericMethod(closedArguments);
        }
        catch (ArgumentException exception)
        {
            throw new ExpressionCastException(
                $"'{node.Method.Name}' cannot be re-bound with the cast argument types.", exception);
        }

        return RebuildCall(closedMethod, instance, arguments!);
    }

    private MethodInfo ResolveInstanceMethod(
        Type receiverType,
        MethodCallExpression node,
        Expression?[] arguments,
        List<(int Index, LambdaExpression Lambda)> deferred)
    {
        foreach (var candidate in ReflectionCache.MethodsNamed(receiverType, node.Method.Name))
        {
            if (candidate.IsStatic || candidate.IsGenericMethodDefinition)
            {
                continue;
            }

            var parameters = candidate.GetParameters();
            if (parameters.Length != arguments.Length)
            {
                continue;
            }

            var compatible = true;
            for (var i = 0; i < arguments.Length && compatible; i++)
            {
                if (arguments[i] is { } known)
                {
                    compatible = parameters[i].ParameterType.IsAssignableFrom(known.Type);
                }
            }

            if (compatible)
            {
                return candidate;
            }
        }

        throw new ExpressionCastException(
            $"Method '{node.Method.DeclaringType!.Name}.{node.Method.Name}' has no counterpart on '{receiverType.Name}'.");
    }

    private LambdaExpression RetypeLambda(LambdaExpression lambda, Type[] parameterTypes)
    {
        var parameters = new ParameterExpression[lambda.Parameters.Count];
        var replaced = new List<ParameterExpression>(lambda.Parameters.Count);

        for (var i = 0; i < lambda.Parameters.Count; i++)
        {
            var original = lambda.Parameters[i];
            var expected = i < parameterTypes.Length ? parameterTypes[i] : original.Type;

            if (expected == original.Type)
            {
                parameters[i] = original;
                continue;
            }

            parameters[i] = Expression.Parameter(expected, original.Name);
            _parameters[original] = parameters[i];
            replaced.Add(original);
        }

        try
        {
            var body = Visit(lambda.Body)!;
            return Expression.Lambda(body, lambda.Name, lambda.TailCall, parameters);
        }
        finally
        {
            foreach (var original in replaced)
            {
                _parameters.Remove(original);
            }
        }
    }

    private static Expression RebuildCall(MethodInfo method, Expression? instance, Expression[] arguments) =>
        instance is null ? Expression.Call(method, arguments) : Expression.Call(instance, method, arguments);

    /// <summary>Lambda parameter types dictated by a closed (non-generic) delegate/Expression parameter.</summary>
    private static Type[] ExpectedParameterTypes(Type parameterType, LambdaExpression lambda)
    {
        var invoke = InferenceRulesUnwrap(parameterType).GetMethod("Invoke");
        return invoke is null
            ? lambda.Parameters.Select(p => p.Type).ToArray()
            : invoke.GetParameters().Select(p => p.ParameterType).ToArray();
    }

    private static Type InferenceRulesUnwrap(Type parameterType) =>
        parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(Expression<>)
            ? parameterType.GetGenericArguments()[0]
            : parameterType;

    private static Type[] DelegateParameterTypes(Type delegateShape, IReadOnlyDictionary<Type, Type> bindings, int count, out bool closed)
    {
        closed = true;
        var invoke = (delegateShape.IsGenericType ? delegateShape.GetGenericTypeDefinition() : delegateShape).GetMethod("Invoke");
        if (invoke is null)
        {
            closed = false;
            return [];
        }

        Dictionary<Type, Type>? map = null;
        if (delegateShape.IsGenericType)
        {
            var definitionArguments = delegateShape.GetGenericTypeDefinition().GetGenericArguments();
            var actualArguments = delegateShape.GetGenericArguments();
            map = new Dictionary<Type, Type>();
            for (var i = 0; i < definitionArguments.Length; i++)
            {
                map[definitionArguments[i]] = actualArguments[i];
            }
        }

        var invokeParameters = invoke.GetParameters();
        var result = new Type[Math.Min(count, invokeParameters.Length)];

        for (var i = 0; i < result.Length; i++)
        {
            var parameterType = invokeParameters[i].ParameterType;
            if (map is not null && map.TryGetValue(parameterType, out var mapped))
            {
                parameterType = mapped;
            }

            parameterType = GenericTypeUnifier.Substitute(parameterType, bindings, out var parameterClosed);
            closed &= parameterClosed;
            result[i] = parameterType;
        }

        return result;
    }

    private static ExpressionCastException RebindFailure(MethodCallExpression node, Type argumentType, int index) =>
        new($"Argument {index} of '{node.Method.Name}' has type '{argumentType}' after casting, " +
            "which does not fit the method signature. Add an explicit CastOptions.Map(...) that preserves the value type.");

    // ---------------------------------------------------------------- object creation & guards

    protected override Expression VisitNew(NewExpression node)
    {
        var arguments = node.Arguments.Select(a => Visit(a)!).ToList();

        var typesChanged = false;
        for (var i = 0; i < arguments.Count; i++)
        {
            typesChanged |= arguments[i].Type != node.Arguments[i].Type;
        }

        if (!typesChanged)
        {
            return node.Update(arguments);
        }

        if ((AnonymousTypeFactory.IsAnonymous(node.Type) || AnonymousTypeFactory.IsGenerated(node.Type))
            && node.Members is not null)
        {
            var shape = new List<KeyValuePair<string, Type>>(arguments.Count);
            for (var i = 0; i < arguments.Count; i++)
            {
                shape.Add(new KeyValuePair<string, Type>(node.Members[i].Name, arguments[i].Type));
            }

            var emitted = AnonymousTypeFactory.Shared.GetOrCreate(shape);
            var constructor = emitted.GetConstructors()[0];
            var members = shape.Select(p => (MemberInfo)emitted.GetProperty(p.Key)!).ToArray();

            return Expression.New(constructor, arguments, members);
        }

        throw new ExpressionCastException(
            $"Constructor of '{node.Type.Name}' received remapped argument types; " +
            "constructing source-side types cannot be cast. Project into an anonymous type instead.");
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (node.Value is not null && _registry.IsSourceType(node.Type))
        {
            throw new ExpressionCastException(
                $"A constant of source type '{node.Type.Name}' cannot be cast; compare scalar members instead of whole instances.");
        }

        return base.VisitConstant(node);
    }
}
