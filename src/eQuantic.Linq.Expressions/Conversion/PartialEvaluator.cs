using System.Collections;
using System.Linq.Expressions;

namespace eQuantic.Linq.Expressions.Conversion;

/// <summary>
/// Funcletizer: evaluates every maximal sub-tree that does not depend on lambda parameters and replaces it
/// with a constant. This folds compiler-generated closures (<c>&lt;&gt;c__DisplayClass</c> captures, <c>this</c>
/// references) into serializable values while deliberately preserving structural nodes (object creation,
/// lambdas, quotes, statement trees) so the reconstructed expression keeps its shape.
/// </summary>
internal static class PartialEvaluator
{
    public static Expression Eval(Expression expression)
    {
        var candidates = new Nominator().Nominate(expression);
        return new SubtreeEvaluator(candidates).Visit(expression)!;
    }

    private static bool CanBeEvaluatedLocally(Expression expression)
    {
        switch (expression.NodeType)
        {
            case ExpressionType.Parameter:
            case ExpressionType.Lambda:
            case ExpressionType.Quote:
            case ExpressionType.Extension:
            case ExpressionType.Dynamic:
            case ExpressionType.Throw:

            // Object creation stays structural: remote providers depend on the shape and
            // per-evaluation instantiation semantics must be preserved.
            case ExpressionType.New:
            case ExpressionType.MemberInit:
            case ExpressionType.ListInit:
            case ExpressionType.NewArrayInit:
            case ExpressionType.NewArrayBounds:
            case ExpressionType.Default:

            // Statement/control-flow nodes are never folded.
            case ExpressionType.Block:
            case ExpressionType.Loop:
            case ExpressionType.Switch:
            case ExpressionType.Try:
            case ExpressionType.Label:
            case ExpressionType.Goto:
            case ExpressionType.DebugInfo:
            case ExpressionType.RuntimeVariables:

            // Assignments have side effects on variables in scope.
            case ExpressionType.Assign:
            case ExpressionType.AddAssign:
            case ExpressionType.AddAssignChecked:
            case ExpressionType.SubtractAssign:
            case ExpressionType.SubtractAssignChecked:
            case ExpressionType.MultiplyAssign:
            case ExpressionType.MultiplyAssignChecked:
            case ExpressionType.DivideAssign:
            case ExpressionType.ModuloAssign:
            case ExpressionType.AndAssign:
            case ExpressionType.OrAssign:
            case ExpressionType.ExclusiveOrAssign:
            case ExpressionType.LeftShiftAssign:
            case ExpressionType.RightShiftAssign:
            case ExpressionType.PowerAssign:
            case ExpressionType.PreIncrementAssign:
            case ExpressionType.PreDecrementAssign:
            case ExpressionType.PostIncrementAssign:
            case ExpressionType.PostDecrementAssign:
                return false;
        }

        // Queryable roots must survive as constants so they can be re-bound remotely.
        if (expression is ConstantExpression { Value: IQueryable })
        {
            return false;
        }

        // Query pipelines carry remote-execution semantics: no Queryable operator — including
        // scalar aggregates like Count()/Sum() — may be collapsed into its local result.
        if (expression is MethodCallExpression call
            && (call.Method.DeclaringType == typeof(Queryable) || typeof(IQueryable).IsAssignableFrom(call.Type)))
        {
            return false;
        }

        // Comparer references (StringComparer.OrdinalIgnoreCase, EqualityComparer<T>.Default, …)
        // must stay structural: folding them would produce constants with no portable value.
        if (IsComparerLike(expression.Type))
        {
            return false;
        }

        return true;
    }

    private static bool IsComparerLike(Type type) =>
        typeof(IComparer).IsAssignableFrom(type)
        || typeof(IEqualityComparer).IsAssignableFrom(type)
        || ImplementsGenericInterface(type, typeof(IComparer<>))
        || ImplementsGenericInterface(type, typeof(IEqualityComparer<>));

    private static bool ImplementsGenericInterface(Type type, Type definition)
    {
        if (type.IsInterface && type.IsGenericType && type.GetGenericTypeDefinition() == definition)
        {
            return true;
        }

        foreach (var contract in type.GetInterfaces())
        {
            if (contract.IsGenericType && contract.GetGenericTypeDefinition() == definition)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Bottom-up pass that marks every node whose whole sub-tree can be evaluated locally.</summary>
    private sealed class Nominator : ExpressionVisitor
    {
        private readonly HashSet<Expression> _candidates = [];
        private bool _cannotBeEvaluated;

        public HashSet<Expression> Nominate(Expression expression)
        {
            Visit(expression);
            return _candidates;
        }

        public override Expression? Visit(Expression? node)
        {
            if (node is null)
            {
                return null;
            }

            var saved = _cannotBeEvaluated;
            _cannotBeEvaluated = false;

            base.Visit(node);

            if (!_cannotBeEvaluated)
            {
                if (CanBeEvaluatedLocally(node))
                {
                    _candidates.Add(node);
                }
                else
                {
                    _cannotBeEvaluated = true;
                }
            }

            _cannotBeEvaluated |= saved;
            return node;
        }
    }

    /// <summary>Top-down pass that replaces maximal nominated sub-trees with their evaluated constants.</summary>
    private sealed class SubtreeEvaluator : ExpressionVisitor
    {
        private readonly HashSet<Expression> _candidates;

        public SubtreeEvaluator(HashSet<Expression> candidates)
        {
            _candidates = candidates;
        }

        public override Expression? Visit(Expression? node)
        {
            if (node is null)
            {
                return null;
            }

            if (_candidates.Contains(node))
            {
                return Evaluate(node);
            }

            return base.Visit(node);
        }

        private static Expression Evaluate(Expression node)
        {
            if (node.NodeType == ExpressionType.Constant || node.Type == typeof(void))
            {
                return node;
            }

            try
            {
                var lambda = Expression.Lambda<Func<object?>>(Expression.Convert(node, typeof(object)));
#if NETSTANDARD2_0
                var factory = lambda.Compile();
#else
                // One-shot evaluation: the interpreter avoids paying JIT compilation per folded sub-tree.
                var factory = lambda.Compile(preferInterpretation: true);
#endif
                return Expression.Constant(factory(), node.Type);
            }
            catch (Exception exception) when (exception is not ExpressionSerializationException)
            {
                throw new ExpressionSerializationException(
                    $"Failed to locally evaluate sub-expression '{node}'. " +
                    "Disable partial evaluation or remove the failing captured value.",
                    exception);
            }
        }
    }
}
