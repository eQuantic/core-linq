using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using eQuantic.Linq.Expressions.Metadata;
using eQuantic.Linq.Expressions.Nodes;
using eQuantic.Linq.Expressions.Resolution;
using eQuantic.Linq.Expressions.Runtime;

namespace eQuantic.Linq.Expressions.Conversion;

/// <summary>
/// Rebuilds a real <see cref="Expression"/> tree from the serializable node model.
/// Fully explicit payloads are rebuilt verbatim; payloads with omitted type information
/// (see <see cref="TypeInfoMode.Minimal"/>) are completed by top-down inference: expected types flow
/// into constants, member owners come from target expressions and method calls are bound by
/// generic unification against the decoded arguments.
/// </summary>
internal sealed class NodeToExpressionConverter
{
    private readonly ExpressionSerializerOptions _options;
    private readonly ITypeResolver _resolver;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly Dictionary<int, ParameterExpression> _parametersById = [];
    private readonly List<Dictionary<string, ParameterExpression>> _scopes = [];
    private readonly Dictionary<int, LabelTarget> _labels = [];

    public NodeToExpressionConverter(ExpressionSerializerOptions options, JsonSerializerOptions jsonOptions)
    {
        _options = options;
        _resolver = options.TypeResolver;
        _jsonOptions = jsonOptions;
    }

    public Expression Convert(ExpressionNode node)
    {
        if (node is null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        return Visit(node, expected: null);
    }

    /// <summary>Rebuilds a lambda from a root-anchored model; the first parameter defaults to <paramref name="rootType"/>.</summary>
    public LambdaExpression ConvertModel(ExpressionModel model, Type rootType)
    {
        if (model.Body is null)
        {
            throw new ExpressionSerializationException("Expression model is missing its body.");
        }

        var declarations = model.Parameters is { Count: > 0 }
            ? model.Parameters
            : [new ParameterNode { Name = "x" }];

        PushScope();
        try
        {
            var parameters = new ParameterExpression[declarations.Count];
            for (var i = 0; i < declarations.Count; i++)
            {
                parameters[i] = DeclareParameter(declarations[i], i == 0 ? rootType : null);
            }

            var body = Visit(model.Body, expected: null);
            return Expression.Lambda(body, parameters);
        }
        finally
        {
            PopScope();
        }
    }

    private int _depth;
    private int _nodeCount;

    private Expression Visit(ExpressionNode node, Type? expected)
    {
        if (++_nodeCount > _options.MaxNodes)
        {
            throw new ExpressionSerializationException(
                $"The payload exceeds the configured MaxNodes limit ({_options.MaxNodes}).");
        }

        if (++_depth > _options.MaxDepth)
        {
            throw new ExpressionSerializationException(
                $"The payload exceeds the configured MaxDepth limit ({_options.MaxDepth}).");
        }

        try
        {
            return VisitCore(node, expected);
        }
        finally
        {
            _depth--;
        }
    }

    private Expression VisitCore(ExpressionNode node, Type? expected) => node switch
    {
        ConstantNode constant => VisitConstant(constant, expected),
        ParameterNode parameter => VisitParameter(parameter),
        LambdaNode lambda => VisitLambda(lambda, expected as Type),
        BinaryNode binary => VisitBinary(binary),
        UnaryNode unary => VisitUnary(unary, expected),
        MethodCallNode call => VisitCall(call),
        MemberNode member => VisitMember(member),
        ConditionalNode conditional => VisitConditional(conditional, expected),
        NewNode @new => VisitNewCore(@new),
        NewArrayNode newArray => VisitNewArray(newArray),
        MemberInitNode memberInit => VisitMemberInit(memberInit),
        ListInitNode listInit => VisitListInit(listInit),
        InvocationNode invocation => VisitInvocation(invocation),
        TypeBinaryNode typeBinary => VisitTypeBinary(typeBinary),
        IndexNode index => VisitIndex(index),
        DefaultNode @default => Expression.Default(Resolve(@default.Type)),
        BlockNode block => VisitBlock(block),
        SwitchNode @switch => VisitSwitch(@switch),
        TryNode @try => VisitTry(@try),
        LoopNode loop => VisitLoop(loop),
        LabelNode label => VisitLabel(label),
        GotoNode @goto => VisitGoto(@goto),
        RuntimeVariablesNode runtimeVariables => VisitRuntimeVariables(runtimeVariables),
        DebugInfoNode debugInfo => VisitDebugInfo(debugInfo),
        QueryRootNode queryRoot => VisitQueryRoot(queryRoot),
        _ => throw new ExpressionSerializationException($"Unknown node type '{node.GetType().Name}'."),
    };

    // ---------------------------------------------------------------- constants

    private Expression VisitConstant(ConstantNode node, Type? expected)
    {
        var staticType = node.Type is null ? null : Resolve(node.Type);

        if (node.Expression is not null)
        {
            var inner = Visit(node.Expression, null);
            return Expression.Constant(inner, staticType ?? inner.GetType());
        }

        var valueType = node.ValueType is null ? null : Resolve(node.ValueType);
        var materializeTarget = valueType ?? staticType ?? Meaningful(expected);

        var value = MaterializeValue(node.Value, materializeTarget);
        var constantType = staticType ?? Meaningful(expected) ?? value?.GetType() ?? typeof(object);

        return Expression.Constant(value, constantType);
    }

    private static Type? Meaningful(Type? expected) => InferenceRules.Meaningful(expected);

    private object? MaterializeValue(object? value, Type? targetType)
    {
        if (value is null)
        {
            return null;
        }

        if (value is JsonElement element)
        {
            if (targetType is null)
            {
                return InferValueFromJson(element);
            }

            try
            {
                return element.Deserialize(targetType, _jsonOptions);
            }
            catch (Exception exception) when (exception is JsonException or NotSupportedException or InvalidOperationException)
            {
                // String payloads (typical for hand-written/query-string sources) get a coercion
                // fallback: "500" → decimal, "true" → bool, "2026-01-01" → DateTime, …
                if (element.ValueKind == JsonValueKind.String)
                {
                    try
                    {
                        return CoerceValue(element.GetString()!, targetType);
                    }
                    catch (ExpressionSerializationException)
                    {
                        // fall through to the original error
                    }
                }

                // Arrays coerce element-wise (query-string membership lists arrive as string arrays).
                if (element.ValueKind == JsonValueKind.Array && ElementTypeOf(targetType) is { } elementType)
                {
                    try
                    {
                        var items = new List<object?>();
                        foreach (var child in element.EnumerateArray())
                        {
                            items.Add(MaterializeValue(child, elementType));
                        }

                        return CoerceValue(items, targetType);
                    }
                    catch (ExpressionSerializationException)
                    {
                        // fall through to the original error
                    }
                }

                throw new ExpressionSerializationException(
                    $"Failed to materialize constant value of type '{targetType}' from JSON.", exception);
            }
        }

        return targetType is null ? value : CoerceValue(value, targetType);
    }

    private static object InferValueFromJson(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString()!,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Number when element.TryGetInt32(out var intValue) => intValue,
        JsonValueKind.Number when element.TryGetInt64(out var longValue) => longValue,
        JsonValueKind.Number => element.GetDouble(),
        _ => throw new ExpressionSerializationException(
            $"Cannot infer the type of a '{element.ValueKind}' constant; add an explicit \"type\" to the constant node."),
    };

    private object CoerceValue(object value, Type targetType)
    {
        if (targetType.IsInstanceOfType(value))
        {
            return value;
        }

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (underlying.IsInstanceOfType(value))
        {
            return value;
        }

        // Collections coerce element-wise (e.g. query-string membership lists arriving as strings).
        if (value is System.Collections.IEnumerable sequence and not string
            && ElementTypeOf(underlying) is { } elementType)
        {
            var items = new List<object?>();
            foreach (var item in sequence)
            {
                items.Add(item is null ? null : CoerceValue(item, elementType));
            }

            var array = Array.CreateInstance(elementType, items.Count);
            for (var i = 0; i < items.Count; i++)
            {
                array.SetValue(items[i], i);
            }

            if (underlying.IsInstanceOfType(array))
            {
                return array;
            }

            var listType = typeof(List<>).MakeGenericType(elementType);
            if (underlying.IsAssignableFrom(listType))
            {
                return Activator.CreateInstance(listType, array)!;
            }
        }

        try
        {
            if (underlying.IsEnum)
            {
                return value is string enumName
                    ? Enum.Parse(underlying, enumName, ignoreCase: true)
                    : Enum.ToObject(underlying, value);
            }

            if (value is string text)
            {
                if (underlying == typeof(Guid))
                {
                    return Guid.Parse(text);
                }

                if (underlying == typeof(TimeSpan))
                {
                    return TimeSpan.Parse(text, _options.FormatProvider);
                }

                if (underlying == typeof(DateTimeOffset))
                {
                    return DateTimeOffset.Parse(text, _options.FormatProvider, DateTimeStyles.RoundtripKind);
                }
            }

            if (value is IConvertible)
            {
                return System.Convert.ChangeType(value, underlying, _options.FormatProvider);
            }
        }
        catch (Exception exception)
        {
            throw new ExpressionSerializationException(
                $"Cannot coerce constant value '{value}' ({value.GetType()}) to '{targetType}'.", exception);
        }

        throw new ExpressionSerializationException(
            $"Cannot coerce constant value '{value}' ({value.GetType()}) to '{targetType}'.");
    }


    private static Type? ElementTypeOf(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            if (definition == typeof(IEnumerable<>) || definition == typeof(IReadOnlyList<>)
                || definition == typeof(IReadOnlyCollection<>) || definition == typeof(IList<>)
                || definition == typeof(ICollection<>) || definition == typeof(List<>))
            {
                return type.GetGenericArguments()[0];
            }
        }

        return null;
    }

    private static bool NeedsTypeContext(ExpressionNode node) => InferenceRules.NeedsTypeContext(node);

    // ---------------------------------------------------------------- parameters & scopes

    private void PushScope() => _scopes.Add(new Dictionary<string, ParameterExpression>(StringComparer.Ordinal));

    private void PopScope() => _scopes.RemoveAt(_scopes.Count - 1);

    private ParameterExpression DeclareParameter(ParameterNode node, Type? fallbackType)
    {
        var type = node.Type is not null
            ? Resolve(node.Type)
            : fallbackType ?? throw new ExpressionSerializationException(
                $"Parameter '{node.Name ?? node.Id.ToString()}' has no type and none could be inferred.");

        if (node.IsByRef && !type.IsByRef)
        {
            type = type.MakeByRefType();
        }

        var parameter = Expression.Parameter(type, node.Name);

        if (!_parametersById.ContainsKey(node.Id))
        {
            _parametersById.Add(node.Id, parameter);
        }

        if (node.Name is not null && _scopes.Count > 0)
        {
            _scopes[_scopes.Count - 1][node.Name] = parameter;
        }

        return parameter;
    }

    private ParameterExpression VisitParameter(ParameterNode node)
    {
        if (_parametersById.TryGetValue(node.Id, out var byId)
            && (node.Name is null || string.Equals(byId.Name, node.Name, StringComparison.Ordinal)))
        {
            return byId;
        }

        if (node.Name is not null)
        {
            for (var i = _scopes.Count - 1; i >= 0; i--)
            {
                if (_scopes[i].TryGetValue(node.Name, out var byName))
                {
                    return byName;
                }
            }
        }

        // Bare parameter reference in a single-parameter scope resolves to that parameter.
        if (node.Name is null && node.Type is null)
        {
            var visible = _scopes.SelectMany(s => s.Values).Distinct().ToList();
            if (visible.Count == 1)
            {
                return visible[0];
            }
        }

        if (node.Type is not null)
        {
            // Legacy/handmade trees may introduce parameters at first use.
            return DeclareParameter(node, null);
        }

        throw new ExpressionSerializationException(
            $"Cannot resolve parameter reference (id: {node.Id}, name: '{node.Name}'). Declare it in a lambda, block or catch scope.");
    }

    // ---------------------------------------------------------------- lambdas

    private LambdaExpression VisitLambda(LambdaNode node, Type? expectedDelegate)
    {
        var delegateType = node.DelegateType is not null ? Resolve(node.DelegateType) : expectedDelegate;
        var invoke = delegateType?.GetMethod("Invoke");
        var invokeParameters = invoke?.GetParameters();

        PushScope();
        try
        {
            var parameters = new ParameterExpression[node.Parameters.Count];
            for (var i = 0; i < node.Parameters.Count; i++)
            {
                var fallback = invokeParameters is not null && i < invokeParameters.Length
                    ? invokeParameters[i].ParameterType
                    : null;
                parameters[i] = DeclareParameter(node.Parameters[i], fallback);
            }

            var bodyExpected = invoke is not null && invoke.ReturnType != typeof(void)
                ? invoke.ReturnType
                : null;

            var body = Visit(RequireBody(node.Body, "lambda"), bodyExpected);

            return delegateType is not null
                ? Expression.Lambda(delegateType, body, node.Name, node.TailCall, parameters)
                : Expression.Lambda(body, node.Name, node.TailCall, parameters);
        }
        finally
        {
            PopScope();
        }
    }

    /// <summary>Decodes a lambda whose parameter types are dictated by the call-site binder (return type free).</summary>
    private LambdaExpression DecodeLambdaWithParameterTypes(LambdaNode node, Type[] parameterTypes)
    {
        PushScope();
        try
        {
            var parameters = new ParameterExpression[node.Parameters.Count];
            for (var i = 0; i < node.Parameters.Count; i++)
            {
                var fallback = i < parameterTypes.Length ? parameterTypes[i] : null;
                parameters[i] = DeclareParameter(node.Parameters[i], fallback);
            }

            var body = Visit(RequireBody(node.Body, "lambda"), null);
            return Expression.Lambda(body, node.Name, node.TailCall, parameters);
        }
        finally
        {
            PopScope();
        }
    }

    // ---------------------------------------------------------------- binary / unary

    private Expression VisitBinary(BinaryNode node)
    {
        var leftNode = RequireBody(node.Left, "binary left operand");
        var rightNode = RequireBody(node.Right, "binary right operand");

        Expression left;
        Expression right;

        var leftNeedsContext = NeedsTypeContext(leftNode);
        var rightNeedsContext = NeedsTypeContext(rightNode);

        if (leftNeedsContext && !rightNeedsContext)
        {
            right = Visit(rightNode, null);
            left = Visit(leftNode, OperandExpectation(node.NodeType, right.Type, isRight: false));
        }
        else
        {
            left = Visit(leftNode, null);
            right = Visit(rightNode, rightNeedsContext ? OperandExpectation(node.NodeType, left.Type, isRight: true) : null);
        }

        var method = node.Method is null ? null : Approve(MemberResolver.ResolveMethod(node.Method, _resolver));
        var conversion = node.Conversion is null ? null : VisitLambda(node.Conversion, null);

        return Expression.MakeBinary(node.NodeType, left, right, node.LiftToNull, method, conversion);
    }

    private static Type? OperandExpectation(ExpressionType nodeType, Type siblingType, bool isRight) =>
        InferenceRules.OperandExpectation(nodeType, siblingType, isRight);

    private Expression VisitUnary(UnaryNode node, Type? expected)
    {
        // Quote is transparent for expectations: the delegate expectation reaches the quoted lambda.
        var operand = node.Operand is null
            ? null
            : Visit(node.Operand, node.NodeType == ExpressionType.Quote ? expected : null);
        var type = node.Type is not null
            ? Resolve(node.Type)
            : node.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked
                ? Meaningful(expected)
                : null;
        var method = node.Method is null ? null : Approve(MemberResolver.ResolveMethod(node.Method, _resolver));

        if (node.NodeType == ExpressionType.Throw)
        {
            return Expression.MakeUnary(ExpressionType.Throw, operand!, type ?? typeof(void), method);
        }

        if (operand is null)
        {
            throw new ExpressionSerializationException($"Unary node '{node.NodeType}' is missing its operand.");
        }

        return Expression.MakeUnary(node.NodeType, operand, type!, method);
    }

    // ---------------------------------------------------------------- calls (explicit + inferred)

    private Expression VisitCall(MethodCallNode node)
    {
        if (node.Method is null)
        {
            throw new ExpressionSerializationException("Call node is missing its method reference.");
        }

        // Fully explicit reference: resolve directly, then decode arguments against the real signature.
        if (node.Method.DeclaringType is not null && node.Method.ParameterTypes is not null)
        {
            var method = Approve(MemberResolver.ResolveMethod(node.Method, _resolver));
            var instance = node.Object is null ? null : Visit(node.Object, null);
            var arguments = DecodeArguments(node.Arguments, method.GetParameters(), 0);
            return Expression.Call(instance, method, arguments);
        }

        return BindCall(node);
    }

    private Expression[] DecodeArguments(List<ExpressionNode>? nodes, ParameterInfo[] parameters, int offset)
    {
        if (nodes is null || nodes.Count == 0)
        {
            return [];
        }

        var expressions = new Expression[nodes.Count];
        for (var i = 0; i < nodes.Count; i++)
        {
            var parameterType = i + offset < parameters.Length ? parameters[i + offset].ParameterType : null;
            expressions[i] = Visit(nodes[i], parameterType is null ? null : UnwrapDelegateExpectation(parameterType));
        }

        return expressions;
    }

    private static Type UnwrapDelegateExpectation(Type parameterType) =>
        InferenceRules.UnwrapDelegateExpectation(parameterType);

    private Expression BindCall(MethodCallNode node)
    {
        var instance = node.Object is null ? null : Visit(node.Object, null);
        var declaringType = node.Method!.DeclaringType is null ? null : Resolve(node.Method.DeclaringType);
        var explicitGenerics = node.Method.GenericArguments?.Select(Resolve).ToArray();
        var argumentNodes = node.Arguments ?? [];

        foreach (var (candidate, shifted) in EnumerateCandidates(node.Method.Name, declaringType, instance))
        {
            if (TryBindCandidate(candidate, shifted, instance, argumentNodes, explicitGenerics, out var method, out var arguments))
            {
                var callInstance = shifted || method.IsStatic ? null : instance;
                return Expression.Call(callInstance, Approve(method), arguments);
            }
        }

        var target = declaringType?.ToString() ?? instance?.Type.ToString() ?? "<unknown>";
        throw new TypeResolutionException(
            $"No overload of '{node.Method.Name}' on '{target}' (or configured extension holders) matches the supplied arguments.");
    }

    private IEnumerable<(MethodInfo Method, bool Shifted)> EnumerateCandidates(string name, Type? declaringType, Expression? instance)
    {
        if (declaringType is not null)
        {
            foreach (var method in ReflectionCache.MethodsNamed(declaringType, name))
            {
                yield return (method, Shifted: false);

                // Static holder + instance-style payload → extension call shape.
                if (instance is not null && method.IsStatic)
                {
                    yield return (method, Shifted: true);
                }
            }

            yield break;
        }

        if (instance is null)
        {
            throw new ExpressionSerializationException(
                $"Call to '{name}' has neither a declaring type nor a target instance to infer from.");
        }

        foreach (var method in ReflectionCache.MethodsNamed(instance.Type, name))
        {
            if (!method.IsStatic)
            {
                yield return (method, Shifted: false);
            }
        }

        foreach (var holder in _options.ExtensionMethodTypes)
        {
            foreach (var method in ReflectionCache.MethodsNamed(holder, name))
            {
                if (method.IsStatic && method.IsPublic)
                {
                    yield return (method, Shifted: true);
                }
            }
        }
    }

    private bool TryBindCandidate(
        MethodInfo candidate,
        bool shifted,
        Expression? instance,
        List<ExpressionNode> argumentNodes,
        Type[]? explicitGenerics,
        out MethodInfo boundMethod,
        out Expression[] boundArguments)
    {
        boundMethod = null!;
        boundArguments = null!;

        var parameters = candidate.GetParameters();
        var slotCount = argumentNodes.Count + (shifted ? 1 : 0);
        if (parameters.Length != slotCount)
        {
            return false;
        }

        var bindings = new Dictionary<Type, Type>();

        if (explicitGenerics is not null)
        {
            if (!candidate.IsGenericMethodDefinition
                || candidate.GetGenericArguments().Length != explicitGenerics.Length)
            {
                return false;
            }

            var open = candidate.GetGenericArguments();
            for (var i = 0; i < open.Length; i++)
            {
                bindings[open[i]] = explicitGenerics[i];
            }
        }

        var arguments = new Expression?[slotCount];
        var deferred = new List<int>();

        if (shifted)
        {
            arguments[0] = instance!;
            if (!GenericTypeUnifier.Unify(parameters[0].ParameterType, instance!.Type, bindings))
            {
                return false;
            }
        }

        for (var slot = shifted ? 1 : 0; slot < slotCount; slot++)
        {
            var argumentNode = argumentNodes[slot - (shifted ? 1 : 0)];
            var parameterType = parameters[slot].ParameterType;
            var substituted = GenericTypeUnifier.Substitute(parameterType, bindings, out var closed);

            if (!closed && (argumentNode is LambdaNode || NeedsTypeContext(argumentNode)))
            {
                deferred.Add(slot);
                continue;
            }

            var expected = closed ? UnwrapDelegateExpectation(substituted) : null;

            Expression argument;
            try
            {
                argument = Visit(argumentNode, expected);
            }
            catch (ExpressionSerializationException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }

            if (!GenericTypeUnifier.Unify(parameterType, argument.Type, bindings))
            {
                return false;
            }

            arguments[slot] = argument;
        }

        // Deferred arguments (open lambdas / context-free constants): iterate until bindings close them.
        var pending = new Queue<int>(deferred);
        var stagnation = 0;
        while (pending.Count > 0)
        {
            if (stagnation > pending.Count)
            {
                return false;
            }

            var slot = pending.Dequeue();
            var argumentNode = argumentNodes[slot - (shifted ? 1 : 0)];
            var parameterType = parameters[slot].ParameterType;
            var substituted = GenericTypeUnifier.Substitute(parameterType, bindings, out var closed);

            if (argumentNode is LambdaNode lambdaNode)
            {
                var delegateShape = UnwrapDelegateExpectation(closed ? substituted : parameterType);
                var invoke = ResolveInvoke(delegateShape);

                if (invoke is null)
                {
                    stagnation++;
                    pending.Enqueue(slot);
                    continue;
                }

                var lambdaParameterTypes = new Type[invoke.Value.ParameterTypes.Length];
                var allClosed = true;
                for (var i = 0; i < lambdaParameterTypes.Length; i++)
                {
                    lambdaParameterTypes[i] = GenericTypeUnifier.Substitute(invoke.Value.ParameterTypes[i], bindings, out var parameterClosed);
                    allClosed &= parameterClosed;
                }

                if (!allClosed)
                {
                    stagnation++;
                    pending.Enqueue(slot);
                    continue;
                }

                Expression lambda;
                try
                {
                    lambda = DecodeLambdaWithParameterTypes(lambdaNode, lambdaParameterTypes);
                }
                catch (ExpressionSerializationException)
                {
                    return false;
                }
                catch (ArgumentException)
                {
                    return false;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }

                if (!GenericTypeUnifier.Unify(parameterType, lambda.Type, bindings))
                {
                    return false;
                }

                arguments[slot] = lambda;
                stagnation = 0;
                continue;
            }

            if (!closed)
            {
                stagnation++;
                pending.Enqueue(slot);
                continue;
            }

            Expression constant;
            try
            {
                constant = Visit(argumentNode, UnwrapDelegateExpectation(substituted));
            }
            catch (ExpressionSerializationException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }

            if (!GenericTypeUnifier.Unify(parameterType, constant.Type, bindings))
            {
                return false;
            }

            arguments[slot] = constant;
            stagnation = 0;
        }

        var method = candidate;
        if (candidate.IsGenericMethodDefinition)
        {
            var open = candidate.GetGenericArguments();
            var closedArguments = new Type[open.Length];
            for (var i = 0; i < open.Length; i++)
            {
                if (!bindings.TryGetValue(open[i], out var bound))
                {
                    return false;
                }

                closedArguments[i] = bound;
            }

            try
            {
                method = candidate.MakeGenericMethod(closedArguments);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        var finalParameters = method.GetParameters();
        for (var i = 0; i < finalParameters.Length; i++)
        {
            if (arguments[i] is null || !ArgumentCompatible(finalParameters[i].ParameterType, arguments[i]!))
            {
                return false;
            }
        }

        boundMethod = method;
        boundArguments = arguments!;
        return true;
    }

    private static bool ArgumentCompatible(Type parameterType, Expression argument)
    {
        if (parameterType.IsAssignableFrom(argument.Type))
        {
            return true;
        }

        // Expression<TDelegate> parameters accept lambdas (Expression.Call quotes them automatically).
        return parameterType.IsGenericType
               && parameterType.GetGenericTypeDefinition() == typeof(Expression<>)
               && argument is LambdaExpression
               && parameterType.GetGenericArguments()[0].IsAssignableFrom(argument.Type);
    }

    private static (Type[] ParameterTypes, Type ReturnType)? ResolveInvoke(Type delegateShape)
    {
        var invoke = delegateShape.IsGenericType
            ? delegateShape.GetGenericTypeDefinition().GetMethod("Invoke")
            : delegateShape.GetMethod("Invoke");

        if (invoke is null)
        {
            return null;
        }

        if (!delegateShape.IsGenericType)
        {
            return (invoke.GetParameters().Select(p => p.ParameterType).ToArray(), invoke.ReturnType);
        }

        // Map the definition's Invoke signature onto this (possibly open) instantiation.
        var definitionArguments = delegateShape.GetGenericTypeDefinition().GetGenericArguments();
        var actualArguments = delegateShape.GetGenericArguments();
        var map = new Dictionary<Type, Type>();
        for (var i = 0; i < definitionArguments.Length; i++)
        {
            map[definitionArguments[i]] = actualArguments[i];
        }

        var parameterTypes = invoke.GetParameters()
            .Select(p => map.TryGetValue(p.ParameterType, out var mapped) ? mapped : p.ParameterType)
            .ToArray();
        var returnType = map.TryGetValue(invoke.ReturnType, out var mappedReturn) ? mappedReturn : invoke.ReturnType;

        return (parameterTypes, returnType);
    }

    // ---------------------------------------------------------------- members

    private Expression VisitMember(MemberNode node)
    {
        if (node.Member is null)
        {
            throw new ExpressionSerializationException("Member node is missing its member reference.");
        }

        var instance = node.Expression is null ? null : Visit(node.Expression, null);

        if (node.Member.DeclaringType is null && instance is null)
        {
            throw new ExpressionSerializationException(
                $"Static member '{node.Member.Name}' requires an explicit declaring type.");
        }

        var member = MemberResolver.ResolveMember(node.Member, _resolver, instance?.Type);
        return Expression.MakeMemberAccess(instance, member);
    }

    // ---------------------------------------------------------------- conditionals

    private Expression VisitConditional(ConditionalNode node, Type? expected)
    {
        var test = Visit(RequireBody(node.Test, "conditional test"), typeof(bool));

        // Branch expectations intentionally ignore the outer expectation — the encoder mirrors this rule.
        _ = expected;
        var branchExpected = node.Type is null ? null : Resolve(node.Type);
        var ifTrue = Visit(RequireBody(node.IfTrue, "conditional ifTrue"), branchExpected);
        var ifFalse = Visit(RequireBody(node.IfFalse, "conditional ifFalse"), branchExpected ?? ifTrue.Type);

        return node.Type is null
            ? Expression.Condition(test, ifTrue, ifFalse)
            : Expression.Condition(test, ifTrue, ifFalse, Resolve(node.Type));
    }

    // ---------------------------------------------------------------- object creation

    private NewExpression VisitNewCore(NewNode node)
    {
        if (node.Type is null)
        {
            throw new ExpressionSerializationException("New node is missing its type.");
        }

        var type = Resolve(node.Type);

        MemberInfo[]? members = null;
        if (node.Members is { Count: > 0 })
        {
            members = node.Members.Select(m => MemberResolver.ResolveMember(m, _resolver, type)).ToArray();
        }

        if (node.Arguments is null or { Count: 0 } && node.Constructor is null)
        {
            return Expression.New(type);
        }

        ConstructorInfo constructor;
        Expression[] arguments;

        if (node.Constructor?.ParameterTypes is not null)
        {
            constructor = MemberResolver.ResolveConstructor(node.Constructor, type, _resolver);
            arguments = DecodeArguments(node.Arguments, constructor.GetParameters(), 0);
        }
        else if (!TryBindConstructor(type, node.Arguments ?? [], out constructor!, out arguments!))
        {
            throw new TypeResolutionException($"No constructor of '{type}' matches the supplied arguments.");
        }

        return members is not null
            ? Expression.New(constructor, arguments, members)
            : Expression.New(constructor, arguments);
    }

    private bool TryBindConstructor(Type type, List<ExpressionNode> argumentNodes, out ConstructorInfo constructor, out Expression[] arguments)
    {
        constructor = null!;
        arguments = null!;

        // Decode context-free arguments once; context-needing constants are decoded per candidate.
        var prototype = new Expression?[argumentNodes.Count];
        for (var i = 0; i < argumentNodes.Count; i++)
        {
            if (!NeedsTypeContext(argumentNodes[i]))
            {
                prototype[i] = Visit(argumentNodes[i], null);
            }
        }

        var candidates = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .OrderBy(c => c.GetParameters().Length)
            .ThenBy(c => c.ToString(), StringComparer.Ordinal);

        foreach (var candidate in candidates)
        {
            var parameters = candidate.GetParameters();
            if (parameters.Length != argumentNodes.Count)
            {
                continue;
            }

            var bound = new Expression[argumentNodes.Count];
            var success = true;

            for (var i = 0; i < argumentNodes.Count && success; i++)
            {
                if (prototype[i] is not null)
                {
                    bound[i] = prototype[i]!;
                    success = parameters[i].ParameterType.IsAssignableFrom(bound[i].Type);
                    continue;
                }

                try
                {
                    bound[i] = Visit(argumentNodes[i], parameters[i].ParameterType);
                    success = parameters[i].ParameterType.IsAssignableFrom(bound[i].Type);
                }
                catch (ExpressionSerializationException)
                {
                    success = false;
                }
            }

            if (success)
            {
                constructor = candidate;
                arguments = bound;
                return true;
            }
        }

        return false;
    }

    private Expression VisitNewArray(NewArrayNode node)
    {
        var elementType = Resolve(node.ElementType);
        var expressions = new Expression[node.Expressions?.Count ?? 0];

        for (var i = 0; i < expressions.Length; i++)
        {
            var expected = node.NodeType == ExpressionType.NewArrayBounds ? typeof(int) : elementType;
            expressions[i] = Visit(node.Expressions![i], expected);
        }

        return node.NodeType == ExpressionType.NewArrayBounds
            ? Expression.NewArrayBounds(elementType, expressions)
            : Expression.NewArrayInit(elementType, expressions);
    }

    private Expression VisitMemberInit(MemberInitNode node)
    {
        var newExpression = VisitNewCore(node.NewExpression);
        var bindings = node.Bindings.Select(b => VisitBinding(b, newExpression.Type)).ToArray();
        return Expression.MemberInit(newExpression, bindings);
    }

    private MemberBinding VisitBinding(MemberBindingNode node, Type contextType)
    {
        var member = MemberResolver.ResolveMember(node.Member, _resolver, contextType);
        var memberType = member is PropertyInfo property ? property.PropertyType : ((FieldInfo)member).FieldType;

        return node switch
        {
            MemberAssignmentNode assignment => Expression.Bind(member, Visit(assignment.Expression, memberType)),
            MemberMemberBindingNode memberMember => Expression.MemberBind(
                member,
                memberMember.Bindings.Select(b => VisitBinding(b, memberType)).ToArray()),
            MemberListBindingNode memberList => Expression.ListBind(
                member,
                memberList.Initializers.Select(i => VisitElementInit(i, memberType)).ToArray()),
            _ => throw new ExpressionSerializationException($"Unknown member binding node '{node.GetType().Name}'."),
        };
    }

    private ElementInit VisitElementInit(ElementInitNode node, Type collectionType)
    {
        if (node.AddMethod is { DeclaringType: not null, ParameterTypes: not null })
        {
            var addMethod = Approve(MemberResolver.ResolveMethod(node.AddMethod, _resolver));
            var arguments = DecodeArguments(node.Arguments, addMethod.GetParameters(), 0);
            return Expression.ElementInit(addMethod, arguments);
        }

        var name = string.IsNullOrEmpty(node.AddMethod?.Name) ? "Add" : node.AddMethod!.Name;

        foreach (var candidate in collectionType.GetMethods(MemberResolver.AllMembers))
        {
            if (!string.Equals(candidate.Name, name, StringComparison.Ordinal)
                || candidate.IsStatic
                || candidate.GetParameters().Length != node.Arguments.Count)
            {
                continue;
            }

            try
            {
                var arguments = DecodeArguments(node.Arguments, candidate.GetParameters(), 0);
                return Expression.ElementInit(Approve(candidate), arguments);
            }
            catch (ExpressionSerializationException)
            {
                // try next overload
            }
            catch (ArgumentException)
            {
                // try next overload
            }
        }

        throw new TypeResolutionException($"No '{name}' method on '{collectionType}' matches the element initializer.");
    }

    private Expression VisitListInit(ListInitNode node)
    {
        var newExpression = VisitNewCore(node.NewExpression);
        var initializers = node.Initializers.Select(i => VisitElementInit(i, newExpression.Type)).ToArray();
        return Expression.ListInit(newExpression, initializers);
    }

    // ---------------------------------------------------------------- invocation / type tests / indexers

    private Expression VisitInvocation(InvocationNode node)
    {
        var target = Visit(RequireBody(node.Expression, "invocation target"), null);
        var invoke = target.Type.GetMethod("Invoke");

        var arguments = invoke is not null
            ? DecodeArguments(node.Arguments, invoke.GetParameters(), 0)
            : (node.Arguments ?? []).Select(a => Visit(a, null)).ToArray();

        return Expression.Invoke(target, arguments);
    }

    private Expression VisitTypeBinary(TypeBinaryNode node)
    {
        var expression = Visit(RequireBody(node.Expression, "type-binary operand"), null);
        var typeOperand = Resolve(node.TypeOperand);

        return node.NodeType == ExpressionType.TypeEqual
            ? Expression.TypeEqual(expression, typeOperand)
            : Expression.TypeIs(expression, typeOperand);
    }

    private Expression VisitIndex(IndexNode node)
    {
        var instance = Visit(RequireBody(node.Object, "index object"), null);

        if (node.Indexer is null)
        {
            var arguments = node.Arguments.Select(a => Visit(a, typeof(int))).ToArray();
            return Expression.ArrayAccess(instance, arguments);
        }

        var indexer = (PropertyInfo)MemberResolver.ResolveMember(node.Indexer, _resolver, instance.Type);
        var indexParameters = indexer.GetIndexParameters();
        var indexArguments = new Expression[node.Arguments.Count];
        for (var i = 0; i < indexArguments.Length; i++)
        {
            var expected = i < indexParameters.Length ? indexParameters[i].ParameterType : null;
            indexArguments[i] = Visit(node.Arguments[i], expected);
        }

        return Expression.MakeIndex(instance, indexer, indexArguments);
    }

    // ---------------------------------------------------------------- statements

    private Expression VisitBlock(BlockNode node)
    {
        PushScope();
        try
        {
            var variables = node.Variables?.Select(v => DeclareParameter(v, null)).ToArray() ?? [];
            var expressions = node.Expressions.Select(e => Visit(e, null)).ToArray();

            return node.Type is null
                ? Expression.Block(variables, expressions)
                : Expression.Block(Resolve(node.Type), variables, expressions);
        }
        finally
        {
            PopScope();
        }
    }

    private Expression VisitSwitch(SwitchNode node)
    {
        var switchValue = Visit(RequireBody(node.SwitchValue, "switch value"), null);
        var cases = node.Cases
            .Select(c => Expression.SwitchCase(
                Visit(c.Body, null),
                c.TestValues.Select(v => Visit(v, switchValue.Type)).ToArray()))
            .ToArray();
        var defaultBody = node.DefaultBody is null ? null : Visit(node.DefaultBody, null);
        var comparison = node.Comparison is null ? null : Approve(MemberResolver.ResolveMethod(node.Comparison, _resolver));
        var type = node.Type is null ? null : Resolve(node.Type);

        return Expression.Switch(type, switchValue, defaultBody, comparison, cases);
    }

    private Expression VisitTry(TryNode node)
    {
        var body = Visit(RequireBody(node.Body, "try body"), null);
        var handlers = node.Handlers?.Select(VisitCatchBlock).ToArray();
        var @finally = node.Finally is null ? null : Visit(node.Finally, null);
        var fault = node.Fault is null ? null : Visit(node.Fault, null);
        var type = node.Type is null ? null : Resolve(node.Type);

        return Expression.MakeTry(type, body, @finally, fault, handlers);
    }

    private CatchBlock VisitCatchBlock(CatchBlockNode node)
    {
        var test = Resolve(node.Test);

        PushScope();
        try
        {
            var variable = node.Variable is null ? null : DeclareParameter(node.Variable, test);
            var body = Visit(RequireBody(node.Body, "catch body"), null);
            var filter = node.Filter is null ? null : Visit(node.Filter, typeof(bool));

            return Expression.MakeCatchBlock(test, variable, body, filter);
        }
        finally
        {
            PopScope();
        }
    }

    private Expression VisitLoop(LoopNode node)
    {
        var body = Visit(RequireBody(node.Body, "loop body"), null);
        var breakLabel = node.BreakLabel is null ? null : LabelOf(node.BreakLabel);
        var continueLabel = node.ContinueLabel is null ? null : LabelOf(node.ContinueLabel);

        return Expression.Loop(body, breakLabel, continueLabel);
    }

    private Expression VisitLabel(LabelNode node)
    {
        var target = LabelOf(node.Target);
        var defaultValue = node.DefaultValue is null ? null : Visit(node.DefaultValue, target.Type == typeof(void) ? null : target.Type);
        return Expression.Label(target, defaultValue);
    }

    private Expression VisitGoto(GotoNode node)
    {
        var target = LabelOf(node.Target);
        var value = node.Value is null ? null : Visit(node.Value, target.Type == typeof(void) ? null : target.Type);
        var type = node.Type is null ? typeof(void) : Resolve(node.Type);

        return Expression.MakeGoto(node.Kind, target, value, type);
    }

    private Expression VisitRuntimeVariables(RuntimeVariablesNode node) =>
        Expression.RuntimeVariables(node.Variables.Select(VisitParameter).ToArray());

    private static Expression VisitDebugInfo(DebugInfoNode node)
    {
        var fileName = node.FileName ?? string.Empty;
        var document = node.Language is null && node.LanguageVendor is null && node.DocumentType is null
            ? Expression.SymbolDocument(fileName)
            : Expression.SymbolDocument(
                fileName,
                node.Language ?? Guid.Empty,
                node.LanguageVendor ?? Guid.Empty,
                node.DocumentType ?? Guid.Empty);

        return node.IsClear
            ? Expression.ClearDebugInfo(document)
            : Expression.DebugInfo(document, node.StartLine, node.StartColumn, node.EndLine, node.EndColumn);
    }

    // ---------------------------------------------------------------- query roots & labels

    private Expression VisitQueryRoot(QueryRootNode node)
    {
        var elementType = Resolve(node.ElementType);
        var provider = _options.QueryRootProvider
                       ?? throw new ExpressionSerializationException(
                           $"The payload contains a queryable root of '{elementType}', but no " +
                           $"{nameof(ExpressionSerializerOptions.QueryRootProvider)} is configured to re-bind it.");

        var queryable = provider(elementType)
                        ?? throw new ExpressionSerializationException(
                            $"{nameof(ExpressionSerializerOptions.QueryRootProvider)} returned null for element type '{elementType}'.");

        var rootExpression = queryable.Expression;

        // Simple providers (EnumerableQuery and friends) expose themselves as a self-referencing
        // constant typed with their runtime type; re-type it with the original static type so the
        // rebuilt tree matches the serialized one exactly. Richer providers (e.g. EF) supply their
        // own root expression, which is spliced verbatim.
        if (rootExpression is ConstantExpression constant && ReferenceEquals(constant.Value, queryable))
        {
            var staticType = node.QueryableType is null ? null : Resolve(node.QueryableType);
            if (staticType is null || !staticType.IsInstanceOfType(queryable))
            {
                staticType = typeof(IQueryable<>).MakeGenericType(elementType);
            }

            return Expression.Constant(queryable, staticType);
        }

        return rootExpression;
    }

    private LabelTarget LabelOf(LabelTargetNode node)
    {
        if (_labels.TryGetValue(node.Id, out var existing))
        {
            return existing;
        }

        var type = node.Type is null ? typeof(void) : Resolve(node.Type);
        var label = Expression.Label(type, node.Name);
        _labels.Add(node.Id, label);
        return label;
    }


    /// <summary>Applies the configured <see cref="ExpressionSerializerOptions.MethodFilter"/> gate.</summary>
    private MethodInfo Approve(MethodInfo method)
    {
        if (_options.MethodFilter is { } filter && !filter(method))
        {
            throw new ExpressionSerializationException(
                $"Method '{method.DeclaringType}.{method.Name}' was rejected by the configured MethodFilter.");
        }

        return method;
    }

    private static T RequireBody<T>(T? value, string what)
        where T : class =>
        value ?? throw new ExpressionSerializationException($"Node is missing its {what}.");

    private Type Resolve(TypeRef typeRef) => _resolver.ResolveType(typeRef);
}
