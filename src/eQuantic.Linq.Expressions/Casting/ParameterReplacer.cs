using System.Linq.Expressions;

namespace eQuantic.Linq.Expressions.Casting;

/// <summary>Substitutes a parameter occurrence with an arbitrary expression (used to inline map bodies).</summary>
internal sealed class ParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _parameter;
    private readonly Expression _replacement;

    private ParameterReplacer(ParameterExpression parameter, Expression replacement)
    {
        _parameter = parameter;
        _replacement = replacement;
    }

    public static Expression Replace(Expression body, ParameterExpression parameter, Expression replacement) =>
        new ParameterReplacer(parameter, replacement).Visit(body)!;

    protected override Expression VisitParameter(ParameterExpression node) =>
        node == _parameter ? _replacement : node;
}
