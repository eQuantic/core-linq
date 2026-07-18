using System.Linq.Expressions;
using System.Reflection;
using eQuantic.Linq.Expressions.Metadata;
using eQuantic.Linq.Expressions.Nodes;
using eQuantic.Linq.Expressions.Resolution;
using eQuantic.Linq.Expressions.Runtime;

namespace eQuantic.Linq.Expressions.Conversion;

/// <summary>
/// Encodes any <see cref="Expression"/> tree into the serializable node model.
/// In <see cref="TypeInfoMode.Minimal"/> it omits every piece of type information the decoder can
/// re-infer (see <see cref="InferenceRules"/>), producing lean root-anchored payloads.
/// </summary>
internal sealed class ExpressionToNodeConverter
{
    private readonly ExpressionSerializerOptions _options;
    private readonly ITypeResolver _resolver;
    private readonly TypeInfoMode _mode;
    private readonly Dictionary<ParameterExpression, int> _parameterIds = [];
    private readonly Dictionary<LabelTarget, int> _labelIds = [];

    public ExpressionToNodeConverter(ExpressionSerializerOptions options, TypeInfoMode mode = TypeInfoMode.Full)
    {
        _options = options;
        _resolver = options.TypeResolver;
        _mode = mode;
    }

    private bool Minimal => _mode == TypeInfoMode.Minimal;

    public ExpressionNode Convert(Expression expression) => Visit(expression, expected: null);

    /// <summary>Fills a root-anchored model from a lambda; the first parameter's type is implied by <paramref name="rootType"/>.</summary>
    public void FillModel(ExpressionModel model, LambdaExpression lambda, Type rootType)
    {
        var parameters = new List<ParameterNode>(lambda.Parameters.Count);
        for (var i = 0; i < lambda.Parameters.Count; i++)
        {
            var parameter = lambda.Parameters[i];
            var omitType = Minimal && i == 0 && parameter.Type == rootType;
            parameters.Add(EncodeParameterDeclaration(parameter, omitType));
        }

        model.Parameters = parameters;

        // ConvertModel decodes the body without a return-type expectation, so none is assumed here.
        model.Body = Visit(lambda.Body, null);
    }

    private ExpressionNode Visit(Expression expression, Type? expected)
    {
        switch (expression)
        {
            case ConstantExpression constant:
                return VisitConstant(constant, expected);
            case ParameterExpression parameter:
                return VisitParameter(parameter);
            case LambdaExpression lambda:
                return VisitLambda(lambda, expected);
            case BinaryExpression binary:
                return VisitBinary(binary);
            case UnaryExpression unary:
                return VisitUnary(unary, expected);
            case MethodCallExpression call:
                return VisitCall(call);
            case MemberExpression member:
                return VisitMember(member);
            case ConditionalExpression conditional:
                return VisitConditional(conditional);
            case NewExpression @new:
                return VisitNew(@new);
            case NewArrayExpression newArray:
                return VisitNewArray(newArray);
            case MemberInitExpression memberInit:
                return VisitMemberInit(memberInit);
            case ListInitExpression listInit:
                return VisitListInit(listInit);
            case InvocationExpression invocation:
                return VisitInvocation(invocation);
            case TypeBinaryExpression typeBinary:
                return VisitTypeBinary(typeBinary);
            case IndexExpression index:
                return VisitIndex(index);
            case DefaultExpression @default:
                return new DefaultNode { Type = Ref(@default.Type) };
            case BlockExpression block:
                return VisitBlock(block);
            case SwitchExpression @switch:
                return VisitSwitch(@switch);
            case TryExpression @try:
                return VisitTry(@try);
            case LoopExpression loop:
                return VisitLoop(loop);
            case LabelExpression label:
                return VisitLabel(label);
            case GotoExpression @goto:
                return VisitGoto(@goto);
            case RuntimeVariablesExpression runtimeVariables:
                return VisitRuntimeVariables(runtimeVariables);
            case DebugInfoExpression debugInfo:
                return VisitDebugInfo(debugInfo);
            case DynamicExpression:
                throw new ExpressionSerializationException(
                    "DynamicExpression cannot be serialized: dynamic call sites carry runtime binders that have no portable representation. " +
                    "Rewrite the expression with static typing before serializing.");
            default:
                if (expression.NodeType == ExpressionType.Extension && expression.CanReduce)
                {
                    return Visit(expression.ReduceAndCheck(), expected);
                }

                throw new ExpressionSerializationException(
                    $"Expression node '{expression.NodeType}' ({expression.GetType().Name}) is not supported.");
        }
    }

    // ---------------------------------------------------------------- constants

    private ExpressionNode VisitConstant(ConstantExpression constant, Type? expected)
    {
        var value = constant.Value;

        if (_options.DetectQueryableRoots && value is IQueryable queryable)
        {
            var canonicalQueryable = typeof(IQueryable<>).MakeGenericType(queryable.ElementType);
            return new QueryRootNode
            {
                ElementType = Ref(queryable.ElementType),
                QueryableType = Minimal && constant.Type == canonicalQueryable ? null : Ref(constant.Type),
            };
        }

        if (value is Expression innerExpression)
        {
            return new ConstantNode
            {
                Type = Ref(constant.Type),
                Expression = Visit(innerExpression, null),
            };
        }

        if (value is Delegate)
        {
            throw new ExpressionSerializationException(
                $"Delegate constant of type '{constant.Type}' cannot be serialized. " +
                "Delegates have no portable representation; keep the logic as an expression tree instead.");
        }

        // Well-known singletons (StringComparer.Ordinal, EqualityComparer<T>.Default, …) serialize
        // as the static member access that exposes them.
        if (value is not null && WellKnownSingletons.TryGetMember(value, out var singleton))
        {
            return new MemberNode { Member = MemberRefOf(singleton, contextType: null) };
        }

        var omitType = Minimal
                       && InferenceRules.Meaningful(expected) == constant.Type
                       && (value is null || value.GetType() == constant.Type);

        var node = new ConstantNode
        {
            Type = omitType ? null : Ref(constant.Type),
            Value = value,
        };

        if (value is not null && value.GetType() != constant.Type)
        {
            node.ValueType = Ref(value.GetType());
        }

        return node;
    }

    // ---------------------------------------------------------------- parameters

    private int IdOf(ParameterExpression parameter)
    {
        if (!_parameterIds.TryGetValue(parameter, out var id))
        {
            id = _parameterIds.Count;
            _parameterIds.Add(parameter, id);
        }

        return id;
    }

    /// <summary>Encodes a parameter occurrence (reference); in Minimal mode identity alone suffices.</summary>
    private ParameterNode VisitParameter(ParameterExpression parameter)
    {
        var node = new ParameterNode
        {
            Id = IdOf(parameter),
            Name = parameter.Name,
        };

        if (!Minimal)
        {
            node.Type = Ref(parameter.Type);
            node.IsByRef = parameter.IsByRef;
        }

        return node;
    }

    private ParameterNode EncodeParameterDeclaration(ParameterExpression parameter, bool omitType)
    {
        var node = new ParameterNode
        {
            Id = IdOf(parameter),
            Name = parameter.Name,
        };

        if (!omitType)
        {
            node.Type = Ref(parameter.Type);
            node.IsByRef = parameter.IsByRef;
        }

        return node;
    }

    // ---------------------------------------------------------------- lambdas

    private LambdaNode VisitLambda(LambdaExpression lambda, Type? expectedDelegate)
    {
        var contextRecoversDelegate = Minimal && expectedDelegate == lambda.Type;
        var canonical = Minimal && !contextRecoversDelegate && IsCanonicalDelegate(lambda);

        var parameters = new List<ParameterNode>(lambda.Parameters.Count);
        foreach (var parameter in lambda.Parameters)
        {
            parameters.Add(EncodeParameterDeclaration(parameter, omitType: contextRecoversDelegate));
        }

        var delegateKnownToDecoder = contextRecoversDelegate || !canonical;

        return new LambdaNode
        {
            DelegateType = contextRecoversDelegate || canonical ? null : Ref(lambda.Type),
            Parameters = parameters,
            Body = Visit(lambda.Body, LambdaBodyExpectation(lambda, delegateKnownToDecoder)),
            Name = lambda.Name,
            TailCall = lambda.TailCall,
        };
    }

    private Type? LambdaBodyExpectation(LambdaExpression lambda, bool delegateKnownToDecoder)
    {
        if (!Minimal || !delegateKnownToDecoder)
        {
            return null;
        }

        return lambda.ReturnType == typeof(void) ? null : lambda.ReturnType;
    }

    private static bool IsCanonicalDelegate(LambdaExpression lambda)
    {
        try
        {
            var signature = new Type[lambda.Parameters.Count + 1];
            for (var i = 0; i < lambda.Parameters.Count; i++)
            {
                signature[i] = lambda.Parameters[i].Type;
            }

            signature[signature.Length - 1] = lambda.ReturnType;
            return Expression.GetDelegateType(signature) == lambda.Type;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    // ---------------------------------------------------------------- binary / unary

    private BinaryNode VisitBinary(BinaryExpression binary)
    {
        ExpressionNode left;
        ExpressionNode right;

        var leftIsConstant = binary.Left is ConstantExpression;
        var rightIsConstant = binary.Right is ConstantExpression;

        // Mirrors the decoder: expectation flows to the constant side only.
        if (Minimal && leftIsConstant && !rightIsConstant)
        {
            right = Visit(binary.Right, null);
            left = Visit(binary.Left, InferenceRules.OperandExpectation(binary.NodeType, binary.Right.Type, isRight: false));
        }
        else
        {
            left = Visit(binary.Left, null);
            right = Visit(
                binary.Right,
                Minimal && rightIsConstant
                    ? InferenceRules.OperandExpectation(binary.NodeType, binary.Left.Type, isRight: true)
                    : null);
        }

        return new BinaryNode
        {
            NodeType = binary.NodeType,
            Left = left,
            Right = right,
            LiftToNull = binary.IsLiftedToNull,
            Method = binary.Method is null || IsCanonicalBinaryMethod(binary)
                ? null
                : MethodRefOf(binary.Method, arguments: null),
            Conversion = binary.Conversion is null ? null : VisitLambda(binary.Conversion, null),
        };
    }

    /// <summary>
    /// Operator methods that <see cref="Expression.MakeBinary(ExpressionType, Expression, Expression)"/>
    /// resolves on its own (e.g. <c>decimal.op_GreaterThan</c>) may be omitted in Minimal mode.
    /// </summary>
    private bool IsCanonicalBinaryMethod(BinaryExpression binary)
    {
        if (!Minimal)
        {
            return false;
        }

        try
        {
            var probe = Expression.MakeBinary(
                binary.NodeType,
                binary.Left,
                binary.Right,
                binary.IsLiftedToNull,
                method: null,
                binary.Conversion);

            return probe.Method == binary.Method;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private UnaryNode VisitUnary(UnaryExpression unary, Type? expected) => new()
    {
        NodeType = unary.NodeType,
        Operand = unary.Operand is null
            ? null
            : Visit(unary.Operand, unary.NodeType == ExpressionType.Quote ? expected : null),
        Type = UnaryCarriesType(unary.NodeType) ? Ref(unary.Type) : null,
        Method = unary.Method is null || IsCanonicalUnaryMethod(unary)
            ? null
            : MethodRefOf(unary.Method, arguments: null),
    };

    private bool IsCanonicalUnaryMethod(UnaryExpression unary)
    {
        if (!Minimal || unary.Operand is null)
        {
            return false;
        }

        try
        {
            var probe = Expression.MakeUnary(unary.NodeType, unary.Operand, unary.Type, method: null);
            return probe.Method == unary.Method;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool UnaryCarriesType(ExpressionType nodeType) => nodeType switch
    {
        ExpressionType.Convert => true,
        ExpressionType.ConvertChecked => true,
        ExpressionType.TypeAs => true,
        ExpressionType.Unbox => true,
        ExpressionType.Throw => true,
        _ => false,
    };

    // ---------------------------------------------------------------- calls

    private MethodCallNode VisitCall(MethodCallExpression call) => new()
    {
        Method = MethodRefOf(call.Method, call.Arguments),
        Object = call.Object is null ? null : Visit(call.Object, null),
        Arguments = VisitArguments(call.Arguments, call.Method),
    };

    private List<ExpressionNode>? VisitArguments(IReadOnlyList<Expression> arguments, MethodBase method)
    {
        if (arguments.Count == 0)
        {
            return null;
        }

        var parameters = method.GetParameters();
        var definitionParameters = method is MethodInfo { IsGenericMethod: true } genericMethod
            ? genericMethod.GetGenericMethodDefinition().GetParameters()
            : parameters;

        var nodes = new List<ExpressionNode>(arguments.Count);
        for (var i = 0; i < arguments.Count; i++)
        {
            nodes.Add(Visit(arguments[i], ArgumentExpectation(arguments[i], parameters, definitionParameters, i)));
        }

        return nodes;
    }

    /// <summary>
    /// Expectation for a call argument. Lambdas always receive the closed delegate expectation
    /// (the decoder's binder recovers it by unification); constants only when the parameter type is
    /// closed in the method definition — otherwise the decoder could not re-infer them before binding.
    /// </summary>
    private Type? ArgumentExpectation(Expression argument, ParameterInfo[] parameters, ParameterInfo[] definitionParameters, int index)
    {
        if (!Minimal || index >= parameters.Length)
        {
            return null;
        }

        var closedType = InferenceRules.UnwrapDelegateExpectation(parameters[index].ParameterType);

        if (argument is LambdaExpression || argument is UnaryExpression { NodeType: ExpressionType.Quote })
        {
            return closedType;
        }

        return definitionParameters[index].ParameterType.ContainsGenericParameters ? null : closedType;
    }

    // ---------------------------------------------------------------- members

    private MemberNode VisitMember(MemberExpression member)
    {
        var target = member.Expression is null ? null : Visit(member.Expression, null);

        return new MemberNode
        {
            Member = MemberRefOf(member.Member, contextType: member.Expression?.Type),
            Expression = target,
        };
    }

    // ---------------------------------------------------------------- conditionals

    private ConditionalNode VisitConditional(ConditionalExpression conditional)
    {
        var omitType = Minimal && conditional.IfTrue.Type == conditional.IfFalse.Type && conditional.Type == conditional.IfTrue.Type;

        // Mirrors the decoder: with the type omitted, ifFalse is decoded expecting ifTrue's type.
        var ifTrue = Visit(conditional.IfTrue, omitType ? null : (Minimal ? conditional.Type : null));
        var ifFalse = Visit(conditional.IfFalse, Minimal ? (omitType ? conditional.IfTrue.Type : conditional.Type) : null);

        return new ConditionalNode
        {
            Test = Visit(conditional.Test, Minimal ? typeof(bool) : null),
            IfTrue = ifTrue,
            IfFalse = ifFalse,
            Type = omitType ? null : Ref(conditional.Type),
        };
    }

    // ---------------------------------------------------------------- object creation

    private NewNode VisitNew(NewExpression @new)
    {
        var node = new NewNode
        {
            Type = Ref(@new.Type),
            Constructor = @new.Constructor is null ? null : ConstructorRefOf(@new.Constructor, @new.Type),
            Members = @new.Members?.Select(m => MemberRefOf(m, contextType: @new.Type)).ToList(),
        };

        if (@new.Arguments.Count > 0)
        {
            var parameters = @new.Constructor?.GetParameters() ?? [];
            var arguments = new List<ExpressionNode>(@new.Arguments.Count);
            for (var i = 0; i < @new.Arguments.Count; i++)
            {
                var expected = Minimal && i < parameters.Length ? parameters[i].ParameterType : null;
                arguments.Add(Visit(@new.Arguments[i], expected));
            }

            node.Arguments = arguments;
        }

        return node;
    }

    private NewArrayNode VisitNewArray(NewArrayExpression newArray)
    {
        var elementType = newArray.Type.GetElementType()!;
        var expected = newArray.NodeType == ExpressionType.NewArrayBounds ? typeof(int) : elementType;

        return new NewArrayNode
        {
            NodeType = newArray.NodeType,
            ElementType = Ref(elementType),
            Expressions = newArray.Expressions.Count == 0
                ? null
                : newArray.Expressions.Select(e => Visit(e, Minimal ? expected : null)).ToList(),
        };
    }

    private MemberInitNode VisitMemberInit(MemberInitExpression memberInit) => new()
    {
        NewExpression = VisitNew(memberInit.NewExpression),
        Bindings = memberInit.Bindings.Select(b => VisitBinding(b, memberInit.NewExpression.Type)).ToList(),
    };

    private MemberBindingNode VisitBinding(MemberBinding binding, Type contextType)
    {
        var memberType = binding.Member is PropertyInfo property
            ? property.PropertyType
            : ((FieldInfo)binding.Member).FieldType;

        return binding switch
        {
            MemberAssignment assignment => new MemberAssignmentNode
            {
                Member = MemberRefOf(assignment.Member, contextType),
                Expression = Visit(assignment.Expression, Minimal ? memberType : null),
            },
            MemberMemberBinding memberMember => new MemberMemberBindingNode
            {
                Member = MemberRefOf(memberMember.Member, contextType),
                Bindings = memberMember.Bindings.Select(b => VisitBinding(b, memberType)).ToList(),
            },
            MemberListBinding memberList => new MemberListBindingNode
            {
                Member = MemberRefOf(memberList.Member, contextType),
                Initializers = memberList.Initializers.Select(i => VisitElementInit(i, memberType)).ToList(),
            },
            _ => throw new ExpressionSerializationException($"Member binding '{binding.BindingType}' is not supported."),
        };
    }

    private ElementInitNode VisitElementInit(ElementInit elementInit, Type collectionType)
    {
        var method = elementInit.AddMethod;
        var omitOwner = Minimal
                        && method.DeclaringType is not null
                        && method.DeclaringType.IsAssignableFrom(collectionType)
                        && CountAddOverloads(collectionType, method.Name, method.GetParameters().Length) == 1;

        var parameters = method.GetParameters();
        var arguments = new List<ExpressionNode>(elementInit.Arguments.Count);
        for (var i = 0; i < elementInit.Arguments.Count; i++)
        {
            var expected = Minimal && i < parameters.Length ? parameters[i].ParameterType : null;
            arguments.Add(Visit(elementInit.Arguments[i], expected));
        }

        return new ElementInitNode
        {
            AddMethod = omitOwner
                ? new MethodRef { Name = method.Name }
                : MethodRefOf(method, arguments: null),
            Arguments = arguments,
        };
    }

    private static int CountAddOverloads(Type collectionType, string name, int parameterCount)
    {
        var count = 0;
        foreach (var method in ReflectionCache.MethodsNamed(collectionType, name))
        {
            if (!method.IsStatic && method.GetParameters().Length == parameterCount)
            {
                count++;
            }
        }

        return count;
    }

    private ListInitNode VisitListInit(ListInitExpression listInit) => new()
    {
        NewExpression = VisitNew(listInit.NewExpression),
        Initializers = listInit.Initializers.Select(i => VisitElementInit(i, listInit.NewExpression.Type)).ToList(),
    };

    // ---------------------------------------------------------------- invocation / type tests / indexers

    private InvocationNode VisitInvocation(InvocationExpression invocation)
    {
        var invoke = invocation.Expression.Type.GetMethod("Invoke");
        var parameters = invoke?.GetParameters() ?? [];

        var arguments = new List<ExpressionNode>(invocation.Arguments.Count);
        for (var i = 0; i < invocation.Arguments.Count; i++)
        {
            var expected = Minimal && i < parameters.Length
                ? InferenceRules.UnwrapDelegateExpectation(parameters[i].ParameterType)
                : null;
            arguments.Add(Visit(invocation.Arguments[i], expected));
        }

        return new InvocationNode
        {
            Expression = Visit(invocation.Expression, null),
            Arguments = arguments.Count == 0 ? null : arguments,
        };
    }

    private TypeBinaryNode VisitTypeBinary(TypeBinaryExpression typeBinary) => new()
    {
        NodeType = typeBinary.NodeType,
        Expression = Visit(typeBinary.Expression, null),
        TypeOperand = Ref(typeBinary.TypeOperand),
    };

    private IndexNode VisitIndex(IndexExpression index)
    {
        var parameters = index.Indexer?.GetIndexParameters() ?? [];

        var arguments = new List<ExpressionNode>(index.Arguments.Count);
        for (var i = 0; i < index.Arguments.Count; i++)
        {
            var expected = Minimal
                ? (index.Indexer is null ? typeof(int) : i < parameters.Length ? parameters[i].ParameterType : null)
                : null;
            arguments.Add(Visit(index.Arguments[i], expected));
        }

        return new IndexNode
        {
            Object = Visit(index.Object!, null),
            Indexer = index.Indexer is null ? null : MemberRefOf(index.Indexer, contextType: index.Object?.Type),
            Arguments = arguments,
        };
    }

    // ---------------------------------------------------------------- statements

    private BlockNode VisitBlock(BlockExpression block) => new()
    {
        Type = Ref(block.Type),
        Variables = block.Variables.Count == 0
            ? null
            : block.Variables.Select(v => EncodeParameterDeclaration(v, omitType: false)).ToList(),
        Expressions = block.Expressions.Select(e => Visit(e, null)).ToList(),
    };

    private SwitchNode VisitSwitch(SwitchExpression @switch) => new()
    {
        Type = Ref(@switch.Type),
        SwitchValue = Visit(@switch.SwitchValue, null),
        Cases = @switch.Cases.Select(c => new SwitchCaseNode
        {
            TestValues = c.TestValues.Select(v => Visit(v, Minimal ? @switch.SwitchValue.Type : null)).ToList(),
            Body = Visit(c.Body, null),
        }).ToList(),
        DefaultBody = @switch.DefaultBody is null ? null : Visit(@switch.DefaultBody, null),
        Comparison = @switch.Comparison is null ? null : MethodRefOf(@switch.Comparison, arguments: null),
    };

    private TryNode VisitTry(TryExpression @try) => new()
    {
        Type = Ref(@try.Type),
        Body = Visit(@try.Body, null),
        Handlers = @try.Handlers.Count == 0 ? null : @try.Handlers.Select(VisitCatchBlock).ToList(),
        Finally = @try.Finally is null ? null : Visit(@try.Finally, null),
        Fault = @try.Fault is null ? null : Visit(@try.Fault, null),
    };

    private CatchBlockNode VisitCatchBlock(CatchBlock catchBlock) => new()
    {
        Test = Ref(catchBlock.Test),
        Variable = catchBlock.Variable is null ? null : EncodeParameterDeclaration(catchBlock.Variable, omitType: false),
        Body = Visit(catchBlock.Body, null),
        Filter = catchBlock.Filter is null ? null : Visit(catchBlock.Filter, Minimal ? typeof(bool) : null),
    };

    private LoopNode VisitLoop(LoopExpression loop) => new()
    {
        Body = Visit(loop.Body, null),
        BreakLabel = loop.BreakLabel is null ? null : LabelOf(loop.BreakLabel),
        ContinueLabel = loop.ContinueLabel is null ? null : LabelOf(loop.ContinueLabel),
    };

    private LabelNode VisitLabel(LabelExpression label) => new()
    {
        Target = LabelOf(label.Target),
        DefaultValue = label.DefaultValue is null
            ? null
            : Visit(label.DefaultValue, Minimal && label.Target.Type != typeof(void) ? label.Target.Type : null),
    };

    private GotoNode VisitGoto(GotoExpression @goto) => new()
    {
        Kind = @goto.Kind,
        Target = LabelOf(@goto.Target),
        Value = @goto.Value is null
            ? null
            : Visit(@goto.Value, Minimal && @goto.Target.Type != typeof(void) ? @goto.Target.Type : null),
        Type = Ref(@goto.Type),
    };

    private RuntimeVariablesNode VisitRuntimeVariables(RuntimeVariablesExpression runtimeVariables) => new()
    {
        Variables = runtimeVariables.Variables.Select(VisitParameter).ToList(),
    };

    private static DebugInfoNode VisitDebugInfo(DebugInfoExpression debugInfo) => new()
    {
        FileName = debugInfo.Document.FileName,
        Language = NullIfEmpty(debugInfo.Document.Language),
        LanguageVendor = NullIfEmpty(debugInfo.Document.LanguageVendor),
        DocumentType = NullIfEmpty(debugInfo.Document.DocumentType),
        StartLine = debugInfo.StartLine,
        StartColumn = debugInfo.StartColumn,
        EndLine = debugInfo.EndLine,
        EndColumn = debugInfo.EndColumn,
        IsClear = debugInfo.IsClear,
    };

    private static Guid? NullIfEmpty(Guid value) => value == Guid.Empty ? null : value;

    private LabelTargetNode LabelOf(LabelTarget target)
    {
        if (!_labelIds.TryGetValue(target, out var id))
        {
            id = _labelIds.Count;
            _labelIds.Add(target, id);
        }

        return new LabelTargetNode
        {
            Id = id,
            Name = target.Name,
            Type = Ref(target.Type),
        };
    }

    // ---------------------------------------------------------------- references

    private TypeRef Ref(Type type) => _resolver.GetTypeRef(type);

    private MethodRef MethodRefOf(MethodInfo method, IReadOnlyList<Expression>? arguments)
    {
        var reference = new MethodRef
        {
            DeclaringType = Ref(method.DeclaringType!),
            Name = method.Name,
            GenericArguments = method.IsGenericMethod
                ? method.GetGenericArguments().Select(Ref).ToList()
                : null,
        };

        var omitParameterTypes = Minimal && arguments is not null && OverloadShapeIsUnique(method);
        if (!omitParameterTypes)
        {
            reference.ParameterTypes = method.GetParameters().Select(p => Ref(p.ParameterType)).ToList();
        }

        return reference;
    }

    /// <summary>
    /// Parameter types may be omitted only when no sibling overload shares the method's name,
    /// generic arity, parameter count and per-parameter delegate shape — the exact signals the
    /// decoder's binder uses to disambiguate.
    /// </summary>
    private static bool OverloadShapeIsUnique(MethodInfo method)
    {
        var template = method.IsGenericMethod ? method.GetGenericMethodDefinition() : method;
        var genericArity = template.IsGenericMethodDefinition ? template.GetGenericArguments().Length : 0;
        var shape = ShapeOf(template);

        var matches = 0;
        foreach (var candidate in ReflectionCache.MethodsNamed(method.DeclaringType!, method.Name))
        {
            var candidateArity = candidate.IsGenericMethodDefinition ? candidate.GetGenericArguments().Length : 0;
            if (candidateArity != genericArity)
            {
                continue;
            }

            if (ShapeOf(candidate) == shape && ++matches > 1)
            {
                return false;
            }
        }

        return matches == 1;
    }

    private static string ShapeOf(MethodBase method)
    {
        var parameters = method.GetParameters();
        var shape = new System.Text.StringBuilder(parameters.Length * 2);

        foreach (var parameter in parameters)
        {
            shape.Append(DelegateArity(parameter.ParameterType)?.ToString() ?? ".").Append(',');
        }

        return shape.ToString();
    }

    private static int? DelegateArity(Type parameterType)
    {
        var type = parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(Expression<>)
            ? parameterType.GetGenericArguments()[0]
            : parameterType;

        if (type.IsGenericType && !type.IsGenericTypeDefinition)
        {
            type = type.GetGenericTypeDefinition();
        }

        return typeof(Delegate).IsAssignableFrom(type)
            ? type.GetMethod("Invoke")?.GetParameters().Length
            : null;
    }

    private MemberRef MemberRefOf(MemberInfo member, Type? contextType) => member switch
    {
        FieldInfo field => BuildMemberRef(field, MemberKind.Field, contextType, indexParameters: null),
        PropertyInfo property => BuildMemberRef(
            property,
            MemberKind.Property,
            contextType,
            property.GetIndexParameters() is { Length: > 0 } indexParameters ? indexParameters : null),
        MethodInfo accessor => MemberRefOfAccessor(accessor, contextType),
        _ => throw new ExpressionSerializationException($"Member '{member}' of type {member.MemberType} is not supported."),
    };

    private MemberRef BuildMemberRef(MemberInfo member, MemberKind kind, Type? contextType, ParameterInfo[]? indexParameters)
    {
        var omitOwner = Minimal
                        && indexParameters is null
                        && contextType is not null
                        && MemberResolver.FindOnType(contextType, member.Name, kind: null) is { } found
                        && MemberResolver.SameMember(found, member);

        return new MemberRef
        {
            DeclaringType = omitOwner ? null : Ref(member.DeclaringType!),
            Name = member.Name,
            Kind = omitOwner ? null : kind,
            ParameterTypes = indexParameters?.Select(p => Ref(p.ParameterType)).ToList(),
        };
    }

    private MemberRef MemberRefOfAccessor(MethodInfo accessor, Type? contextType)
    {
        // NewExpression.Members may reference property get accessors instead of the properties themselves.
        foreach (var property in accessor.DeclaringType!.GetProperties(MemberResolver.AllMembers))
        {
            if (property.GetMethod == accessor || property.SetMethod == accessor)
            {
                return MemberRefOf(property, contextType);
            }
        }

        throw new ExpressionSerializationException($"Accessor method '{accessor}' could not be mapped back to a property.");
    }

    private ConstructorRef ConstructorRefOf(ConstructorInfo constructor, Type constructedType)
    {
        var omitParameterTypes = Minimal && CountConstructorsWithArity(constructedType, constructor.GetParameters().Length) == 1;

        return new ConstructorRef
        {
            DeclaringType = Minimal ? null : Ref(constructor.DeclaringType!),
            ParameterTypes = omitParameterTypes
                ? null
                : constructor.GetParameters().Select(p => Ref(p.ParameterType)).ToList(),
        };
    }

    private static int CountConstructorsWithArity(Type type, int parameterCount)
    {
        var count = 0;
        foreach (var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (constructor.GetParameters().Length == parameterCount)
            {
                count++;
            }
        }

        return count;
    }
}
