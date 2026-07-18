using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace eQuantic.Linq.Expressions.Comparison;

/// <summary>
/// Structural equality comparer for expression trees: two trees are equal when they have the same shape,
/// operators, types, members and constant values, regardless of parameter/label instance identity.
/// </summary>
public sealed class ExpressionEqualityComparer : IEqualityComparer<Expression?>
{
    private static readonly JsonSerializerOptions FallbackJson = new();

    /// <summary>Shared instance.</summary>
    public static ExpressionEqualityComparer Instance { get; } = new();

    /// <inheritdoc />
    public bool Equals(Expression? x, Expression? y) => new Comparison().AreEqual(x, y);

    /// <inheritdoc />
    public int GetHashCode(Expression? obj)
    {
        if (obj is null)
        {
            return 0;
        }

        var visitor = new HashVisitor();
        visitor.Visit(obj);
        return visitor.Hash;
    }

    private sealed class HashVisitor : ExpressionVisitor
    {
        public int Hash { get; private set; } = 17;

        public override Expression? Visit(Expression? node)
        {
            if (node is not null)
            {
                unchecked
                {
                    Hash = (Hash * 31) + (int)node.NodeType;
                    Hash = (Hash * 31) + node.Type.GetHashCode();
                }
            }

            return base.Visit(node);
        }
    }

    private sealed class Comparison
    {
        private readonly Dictionary<ParameterExpression, ParameterExpression> _parameterMap = [];
        private readonly Dictionary<LabelTarget, LabelTarget> _labelMap = [];

        public bool AreEqual(Expression? x, Expression? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            if (x.NodeType != y.NodeType || x.Type != y.Type)
            {
                return false;
            }

            return (x, y) switch
            {
                (ConstantExpression a, ConstantExpression b) => ValuesEqual(a.Value, b.Value),
                (ParameterExpression a, ParameterExpression b) => ParametersEqual(a, b),
                (LambdaExpression a, LambdaExpression b) => LambdasEqual(a, b),
                (BinaryExpression a, BinaryExpression b) => BinariesEqual(a, b),
                (UnaryExpression a, UnaryExpression b) => MembersEqual(a.Method, b.Method) && AreEqual(a.Operand, b.Operand),
                (MethodCallExpression a, MethodCallExpression b) => CallsEqual(a, b),
                (MemberExpression a, MemberExpression b) => MembersEqual(a.Member, b.Member) && AreEqual(a.Expression, b.Expression),
                (ConditionalExpression a, ConditionalExpression b) =>
                    AreEqual(a.Test, b.Test) && AreEqual(a.IfTrue, b.IfTrue) && AreEqual(a.IfFalse, b.IfFalse),
                (NewExpression a, NewExpression b) => NewsEqual(a, b),
                (NewArrayExpression a, NewArrayExpression b) => AllEqual(a.Expressions, b.Expressions),
                (MemberInitExpression a, MemberInitExpression b) =>
                    AreEqual(a.NewExpression, b.NewExpression) && BindingListsEqual(a.Bindings, b.Bindings),
                (ListInitExpression a, ListInitExpression b) =>
                    AreEqual(a.NewExpression, b.NewExpression) && InitializerListsEqual(a.Initializers, b.Initializers),
                (InvocationExpression a, InvocationExpression b) =>
                    AreEqual(a.Expression, b.Expression) && AllEqual(a.Arguments, b.Arguments),
                (TypeBinaryExpression a, TypeBinaryExpression b) =>
                    a.TypeOperand == b.TypeOperand && AreEqual(a.Expression, b.Expression),
                (IndexExpression a, IndexExpression b) =>
                    MembersEqual(a.Indexer, b.Indexer) && AreEqual(a.Object, b.Object) && AllEqual(a.Arguments, b.Arguments),
                (DefaultExpression, DefaultExpression) => true,
                (BlockExpression a, BlockExpression b) => BlocksEqual(a, b),
                (SwitchExpression a, SwitchExpression b) => SwitchesEqual(a, b),
                (TryExpression a, TryExpression b) => TriesEqual(a, b),
                (LoopExpression a, LoopExpression b) =>
                    TargetsEqual(a.BreakLabel, b.BreakLabel) && TargetsEqual(a.ContinueLabel, b.ContinueLabel) && AreEqual(a.Body, b.Body),
                (LabelExpression a, LabelExpression b) =>
                    TargetsEqual(a.Target, b.Target) && AreEqual(a.DefaultValue, b.DefaultValue),
                (GotoExpression a, GotoExpression b) =>
                    a.Kind == b.Kind && TargetsEqual(a.Target, b.Target) && AreEqual(a.Value, b.Value),
                (RuntimeVariablesExpression a, RuntimeVariablesExpression b) => AllEqual(a.Variables, b.Variables),
                (DebugInfoExpression a, DebugInfoExpression b) => DebugInfosEqual(a, b),
                _ => false,
            };
        }

        private bool ParametersEqual(ParameterExpression x, ParameterExpression y)
        {
            if (_parameterMap.TryGetValue(x, out var mapped))
            {
                return ReferenceEquals(mapped, y);
            }

            // Free (undeclared) parameters: compare by name/type shape.
            return x.Type == y.Type && string.Equals(x.Name, y.Name, StringComparison.Ordinal) && x.IsByRef == y.IsByRef;
        }

        private bool LambdasEqual(LambdaExpression x, LambdaExpression y)
        {
            if (x.Parameters.Count != y.Parameters.Count
                || x.TailCall != y.TailCall
                || !string.Equals(x.Name, y.Name, StringComparison.Ordinal))
            {
                return false;
            }

            MapParameters(x.Parameters, y.Parameters, out var invalid);
            if (invalid)
            {
                return false;
            }

            try
            {
                return AreEqual(x.Body, y.Body);
            }
            finally
            {
                UnmapParameters(x.Parameters);
            }
        }

        private void MapParameters(IReadOnlyList<ParameterExpression> x, IReadOnlyList<ParameterExpression> y, out bool invalid)
        {
            invalid = false;
            for (var i = 0; i < x.Count; i++)
            {
                if (x[i].Type != y[i].Type || x[i].IsByRef != y[i].IsByRef)
                {
                    invalid = true;
                }

                _parameterMap[x[i]] = y[i];
            }
        }

        private void UnmapParameters(IReadOnlyList<ParameterExpression> x)
        {
            foreach (var parameter in x)
            {
                _parameterMap.Remove(parameter);
            }
        }

        private bool BinariesEqual(BinaryExpression x, BinaryExpression y) =>
            x.IsLiftedToNull == y.IsLiftedToNull
            && MembersEqual(x.Method, y.Method)
            && AreEqual(x.Left, y.Left)
            && AreEqual(x.Right, y.Right)
            && AreEqual(x.Conversion, y.Conversion);

        private bool CallsEqual(MethodCallExpression x, MethodCallExpression y) =>
            MembersEqual(x.Method, y.Method)
            && AreEqual(x.Object, y.Object)
            && AllEqual(x.Arguments, y.Arguments);

        private bool NewsEqual(NewExpression x, NewExpression y)
        {
            if (!MembersEqual(x.Constructor, y.Constructor) || !AllEqual(x.Arguments, y.Arguments))
            {
                return false;
            }

            if (x.Members is null != y.Members is null)
            {
                return false;
            }

            if (x.Members is null || y.Members is null)
            {
                return true;
            }

            if (x.Members.Count != y.Members.Count)
            {
                return false;
            }

            for (var i = 0; i < x.Members.Count; i++)
            {
                if (!MembersEqual(x.Members[i], y.Members[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool BlocksEqual(BlockExpression x, BlockExpression y)
        {
            if (x.Variables.Count != y.Variables.Count || x.Expressions.Count != y.Expressions.Count)
            {
                return false;
            }

            MapParameters(x.Variables, y.Variables, out var invalid);
            if (invalid)
            {
                return false;
            }

            try
            {
                for (var i = 0; i < x.Expressions.Count; i++)
                {
                    if (!AreEqual(x.Expressions[i], y.Expressions[i]))
                    {
                        return false;
                    }
                }

                return true;
            }
            finally
            {
                UnmapParameters(x.Variables);
            }
        }

        private bool SwitchesEqual(SwitchExpression x, SwitchExpression y)
        {
            if (x.Cases.Count != y.Cases.Count
                || !MembersEqual(x.Comparison, y.Comparison)
                || !AreEqual(x.SwitchValue, y.SwitchValue)
                || !AreEqual(x.DefaultBody, y.DefaultBody))
            {
                return false;
            }

            for (var i = 0; i < x.Cases.Count; i++)
            {
                if (!AllEqual(x.Cases[i].TestValues, y.Cases[i].TestValues) || !AreEqual(x.Cases[i].Body, y.Cases[i].Body))
                {
                    return false;
                }
            }

            return true;
        }

        private bool TriesEqual(TryExpression x, TryExpression y)
        {
            if (x.Handlers.Count != y.Handlers.Count
                || !AreEqual(x.Body, y.Body)
                || !AreEqual(x.Finally, y.Finally)
                || !AreEqual(x.Fault, y.Fault))
            {
                return false;
            }

            for (var i = 0; i < x.Handlers.Count; i++)
            {
                if (!CatchBlocksEqual(x.Handlers[i], y.Handlers[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CatchBlocksEqual(CatchBlock x, CatchBlock y)
        {
            if (x.Test != y.Test || x.Variable is null != y.Variable is null)
            {
                return false;
            }

            var scoped = new List<ParameterExpression>(1);
            if (x.Variable is not null && y.Variable is not null)
            {
                if (x.Variable.Type != y.Variable.Type)
                {
                    return false;
                }

                _parameterMap[x.Variable] = y.Variable;
                scoped.Add(x.Variable);
            }

            try
            {
                return AreEqual(x.Body, y.Body) && AreEqual(x.Filter, y.Filter);
            }
            finally
            {
                UnmapParameters(scoped);
            }
        }

        private static bool DebugInfosEqual(DebugInfoExpression x, DebugInfoExpression y) =>
            x.IsClear == y.IsClear
            && x.StartLine == y.StartLine
            && x.StartColumn == y.StartColumn
            && x.EndLine == y.EndLine
            && x.EndColumn == y.EndColumn
            && string.Equals(x.Document.FileName, y.Document.FileName, StringComparison.Ordinal)
            && x.Document.Language == y.Document.Language
            && x.Document.LanguageVendor == y.Document.LanguageVendor
            && x.Document.DocumentType == y.Document.DocumentType;

        private bool TargetsEqual(LabelTarget? x, LabelTarget? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            if (_labelMap.TryGetValue(x, out var mapped))
            {
                return ReferenceEquals(mapped, y);
            }

            if (x.Type != y.Type || !string.Equals(x.Name, y.Name, StringComparison.Ordinal))
            {
                return false;
            }

            _labelMap[x] = y;
            return true;
        }

        private bool BindingListsEqual(IReadOnlyList<MemberBinding> x, IReadOnlyList<MemberBinding> y)
        {
            if (x.Count != y.Count)
            {
                return false;
            }

            for (var i = 0; i < x.Count; i++)
            {
                if (!BindingsEqual(x[i], y[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool BindingsEqual(MemberBinding x, MemberBinding y)
        {
            if (x.BindingType != y.BindingType || !MembersEqual(x.Member, y.Member))
            {
                return false;
            }

            return (x, y) switch
            {
                (MemberAssignment a, MemberAssignment b) => AreEqual(a.Expression, b.Expression),
                (MemberMemberBinding a, MemberMemberBinding b) => BindingListsEqual(a.Bindings, b.Bindings),
                (MemberListBinding a, MemberListBinding b) => InitializerListsEqual(a.Initializers, b.Initializers),
                _ => false,
            };
        }

        private bool InitializerListsEqual(IReadOnlyList<ElementInit> x, IReadOnlyList<ElementInit> y)
        {
            if (x.Count != y.Count)
            {
                return false;
            }

            for (var i = 0; i < x.Count; i++)
            {
                if (!MembersEqual(x[i].AddMethod, y[i].AddMethod) || !AllEqual(x[i].Arguments, y[i].Arguments))
                {
                    return false;
                }
            }

            return true;
        }

        private bool AllEqual(IReadOnlyList<Expression> x, IReadOnlyList<Expression> y)
        {
            if (x.Count != y.Count)
            {
                return false;
            }

            for (var i = 0; i < x.Count; i++)
            {
                if (!AreEqual(x[i], y[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool MembersEqual(MemberInfo? x, MemberInfo? y)
        {
            if (ReferenceEquals(x, y) || (x is null && y is null))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            if (x.Equals(y))
            {
                return true;
            }

            // Same member observed through different reflected types still counts as equal.
            return x.Module == y.Module
                   && x.MetadataToken == y.MetadataToken
                   && x.DeclaringType == y.DeclaringType;
        }

        private bool ValuesEqual(object? x, object? y)
        {
            if (Equals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            if (x is IQueryable || y is IQueryable)
            {
                return false;
            }

            // Constants may themselves hold expression trees (e.g. quoted lambdas evaluated to values).
            if (x is Expression innerX && y is Expression innerY)
            {
                return AreEqual(innerX, innerY);
            }

            if (x is IEnumerable ex and not string && y is IEnumerable ey and not string)
            {
                var ix = ex.GetEnumerator();
                var iy = ey.GetEnumerator();
                while (true)
                {
                    var mx = ix.MoveNext();
                    var my = iy.MoveNext();
                    if (mx != my)
                    {
                        return false;
                    }

                    if (!mx)
                    {
                        return true;
                    }

                    if (!ValuesEqual(ix.Current, iy.Current))
                    {
                        return false;
                    }
                }
            }

            if (x.GetType() != y.GetType())
            {
                return false;
            }

            // Structural fallback for complex constants (POCOs re-materialized from JSON).
            try
            {
                return JsonSerializer.Serialize(x, FallbackJson) == JsonSerializer.Serialize(y, FallbackJson);
            }
            catch (Exception exception) when (exception is JsonException or NotSupportedException)
            {
                return false;
            }
        }
    }
}
