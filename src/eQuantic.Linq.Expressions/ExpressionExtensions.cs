using System.Linq.Expressions;
using eQuantic.Linq.Expressions.Nodes;

namespace eQuantic.Linq.Expressions;

/// <summary>Convenience extensions over <see cref="ExpressionSerializer"/>.</summary>
public static class ExpressionExtensions
{
    /// <summary>Converts the expression into the structured node model.</summary>
    /// <param name="expression">Expression to convert.</param>
    /// <param name="serializer">Serializer to use; <see cref="ExpressionSerializer.Default"/> when omitted.</param>
    public static ExpressionNode ToNode(this Expression expression, ExpressionSerializer? serializer = null) =>
        (serializer ?? ExpressionSerializer.Default).ToNode(expression);

    /// <summary>Serializes the expression to its JSON model.</summary>
    /// <param name="expression">Expression to serialize.</param>
    /// <param name="serializer">Serializer to use; <see cref="ExpressionSerializer.Default"/> when omitted.</param>
    public static string ToJson(this Expression expression, ExpressionSerializer? serializer = null) =>
        (serializer ?? ExpressionSerializer.Default).ToJson(expression);

    /// <summary>Rebuilds the expression represented by the node model.</summary>
    /// <param name="node">Model to convert back.</param>
    /// <param name="serializer">Serializer to use; <see cref="ExpressionSerializer.Default"/> when omitted.</param>
    public static Expression ToExpression(this ExpressionNode node, ExpressionSerializer? serializer = null) =>
        (serializer ?? ExpressionSerializer.Default).ToExpression(node);

    /// <summary>Rebuilds a strongly typed lambda from the node model.</summary>
    /// <typeparam name="TDelegate">Expected delegate type.</typeparam>
    /// <param name="node">Model to convert back.</param>
    /// <param name="serializer">Serializer to use; <see cref="ExpressionSerializer.Default"/> when omitted.</param>
    public static Expression<TDelegate> ToExpression<TDelegate>(this ExpressionNode node, ExpressionSerializer? serializer = null) =>
        (serializer ?? ExpressionSerializer.Default).ToExpression<TDelegate>(node);

    /// <summary>Converts the lambda into a lean, root-anchored model (see <see cref="ExpressionModel{TRoot}"/>).</summary>
    /// <typeparam name="TRoot">Root entity of the lambda's first parameter.</typeparam>
    /// <typeparam name="TResult">Lambda result type.</typeparam>
    /// <param name="expression">Expression to convert.</param>
    /// <param name="serializer">Serializer to use; <see cref="ExpressionSerializer.Default"/> when omitted.</param>
    public static ExpressionModel<TRoot> ToModel<TRoot, TResult>(
        this Expression<Func<TRoot, TResult>> expression,
        ExpressionSerializer? serializer = null) =>
        (serializer ?? ExpressionSerializer.Default).ToModel(expression);
}
