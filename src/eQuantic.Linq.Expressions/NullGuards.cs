using System.Linq.Expressions;

namespace eQuantic.Linq.Expressions;

/// <summary>
/// Rewrites predicates with C# <c>?.</c>-style null propagation for in-memory execution: every
/// member access or instance call whose receiver is a nullable reference deeper than the lambda
/// parameter is guarded, value results are lifted to <see cref="Nullable{T}"/>, and a boolean body
/// coalesces to <see langword="false"/>. Relational providers translate nulls natively and should
/// receive the unguarded tree.
/// </summary>
public static class NullGuards
{
    /// <summary>Applies null propagation to a typed predicate.</summary>
    /// <typeparam name="T">Root entity type.</typeparam>
    /// <param name="predicate">Predicate to guard.</param>
    public static Expression<Func<T, bool>> Apply<T>(Expression<Func<T, bool>> predicate) =>
        (Expression<Func<T, bool>>)Apply((LambdaExpression)predicate);

    /// <summary>Applies null propagation to any lambda (sort keys, selectors, predicates).</summary>
    /// <param name="lambda">Lambda to guard.</param>
    public static LambdaExpression Apply(LambdaExpression lambda)
    {
        if (lambda is null)
        {
            throw new ArgumentNullException(nameof(lambda));
        }

        var body = new Rewriter().Visit(lambda.Body)!;

        // Lifted boolean predicates coalesce to false; other lifted results keep their lifted type
        // unless the delegate demands the original (then we fall back to default(T)).
        if (body.Type != lambda.ReturnType)
        {
            body = lambda.ReturnType == typeof(bool) && body.Type == typeof(bool?)
                ? Expression.Coalesce(body, Expression.Constant(false))
                : Expression.Coalesce(body, Expression.Default(lambda.ReturnType));
        }

        return Expression.Lambda(lambda.Type, body, lambda.Name, lambda.TailCall, lambda.Parameters);
    }

    private sealed class Rewriter : ExpressionVisitor
    {
        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression is null)
            {
                return base.VisitMember(node);
            }

            var receiver = Visit(node.Expression)!;
            return GuardAccess(receiver, r => Expression.MakeMemberAccess(r, node.Member));
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var arguments = node.Arguments.Select(a => Unlift(Visit(a)!, a.Type)).ToArray();

            if (node.Object is null)
            {
                return node.Update(null, arguments);
            }

            var receiver = Visit(node.Object)!;
            return GuardAccess(receiver, r => Expression.Call(r, node.Method, arguments));
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            var left = Visit(node.Left)!;
            var right = Visit(node.Right)!;

            // Align lifted operands (int? vs int) so comparisons become lifted-to-false, like `?.` chains.
            if (left.Type != node.Left.Type || right.Type != node.Right.Type)
            {
                if (left.Type != right.Type)
                {
                    if (Nullable.GetUnderlyingType(left.Type) == right.Type)
                    {
                        right = Expression.Convert(right, left.Type);
                    }
                    else if (Nullable.GetUnderlyingType(right.Type) == left.Type)
                    {
                        left = Expression.Convert(left, right.Type);
                    }
                }

                if (node.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse)
                {
                    left = Unlift(left, typeof(bool));
                    right = Unlift(right, typeof(bool));
                }

                return Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull, node.Method);
            }

            return node.Update(left, node.Conversion, right);
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            var operand = Visit(node.Operand)!;

            if (operand.Type != node.Operand.Type)
            {
                if (node.NodeType == ExpressionType.Not && node.Type == typeof(bool))
                {
                    return Expression.Not(Unlift(operand, typeof(bool)));
                }

                if (node.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
                {
                    var lifted = node.Type.IsValueType && Nullable.GetUnderlyingType(node.Type) is null
                        ? typeof(Nullable<>).MakeGenericType(node.Type)
                        : node.Type;
                    return Expression.Convert(operand, lifted);
                }
            }

            return node.Update(operand);
        }

        /// <summary>Wraps an access so a null receiver yields a lifted null instead of throwing.</summary>
        private static Expression GuardAccess(Expression receiver, Func<Expression, Expression> access)
        {
            bool needsGuard;
            var accessTarget = receiver;

            if (receiver is ParameterExpression || receiver is ConstantExpression { Value: not null })
            {
                needsGuard = false;
            }
            else if (Nullable.GetUnderlyingType(receiver.Type) is not null)
            {
                needsGuard = true;
                accessTarget = Expression.Property(receiver, "Value");
            }
            else
            {
                needsGuard = !receiver.Type.IsValueType;
            }

            var result = access(accessTarget);

            if (!needsGuard)
            {
                return result;
            }

            var lifted = Lift(result);
            return Expression.Condition(
                Expression.Equal(receiver, Expression.Constant(null, receiver.Type)),
                Expression.Default(lifted.Type),
                lifted);
        }

        private static Expression Lift(Expression expression) =>
            expression.Type.IsValueType && Nullable.GetUnderlyingType(expression.Type) is null
                ? Expression.Convert(expression, typeof(Nullable<>).MakeGenericType(expression.Type))
                : expression;

        private static Expression Unlift(Expression expression, Type expected)
        {
            if (expression.Type == expected)
            {
                return expression;
            }

            if (Nullable.GetUnderlyingType(expression.Type) == expected)
            {
                return Expression.Coalesce(expression, Expression.Default(expected));
            }

            return expression;
        }
    }
}
