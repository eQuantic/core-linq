using System.Linq.Expressions;
using eQuantic.Linq.Expressions.Metadata;
using eQuantic.Linq.Expressions.Nodes;

namespace eQuantic.Linq.Expressions;

/// <summary>
/// Serializable lambda document: declared parameters plus a body. The untyped base carries the payload;
/// use <see cref="ExpressionModel{TRoot}"/> to anchor it on a root entity so omitted type information
/// can be inferred top-down.
/// </summary>
public class ExpressionModel
{
    /// <summary>
    /// Declared lambda parameters. Optional: when omitted, a single parameter of the model's root type
    /// is implied (named <c>x</c> by default).
    /// </summary>
    public List<ParameterNode>? Parameters { get; set; }

    /// <summary>The lambda body over the declared parameters.</summary>
    public ExpressionNode Body { get; set; } = null!;

    /// <summary>Optional explicit result type; normally inferred from <see cref="Body"/>.</summary>
    public TypeRef? ResultType { get; set; }
}

/// <summary>
/// Strongly typed expression document anchored on the root entity <typeparamref name="TRoot"/>
/// (e.g. <c>ExpressionModel&lt;Order&gt;</c> for a <c>Where</c> filter over orders). The anchor drives
/// type inference: parameter types, member owners and constant value types are recovered from
/// <typeparamref name="TRoot"/> downwards, so payloads stay lean and can even be written by hand.
/// </summary>
/// <typeparam name="TRoot">Root entity the lambda's first parameter ranges over.</typeparam>
public sealed class ExpressionModel<TRoot> : ExpressionModel
{
    /// <summary>Builds the model for a typed lambda.</summary>
    /// <typeparam name="TResult">Lambda result type.</typeparam>
    /// <param name="expression">Expression to convert.</param>
    /// <param name="serializer">Serializer to use; <see cref="ExpressionSerializer.Default"/> when omitted.</param>
    public static ExpressionModel<TRoot> From<TResult>(
        Expression<Func<TRoot, TResult>> expression,
        ExpressionSerializer? serializer = null) =>
        (serializer ?? ExpressionSerializer.Default).ToModel(expression);

    /// <summary>Rebuilds the lambda with an explicit result type.</summary>
    /// <typeparam name="TResult">Expected result type.</typeparam>
    /// <param name="serializer">Serializer to use; <see cref="ExpressionSerializer.Default"/> when omitted.</param>
    public Expression<Func<TRoot, TResult>> ToExpression<TResult>(ExpressionSerializer? serializer = null) =>
        (serializer ?? ExpressionSerializer.Default).ToExpression<TRoot, TResult>(this);

    /// <summary>Rebuilds the lambda as a predicate (<c>TRoot → bool</c>), the <c>Where</c>-filter shape.</summary>
    /// <param name="serializer">Serializer to use; <see cref="ExpressionSerializer.Default"/> when omitted.</param>
    public Expression<Func<TRoot, bool>> ToPredicate(ExpressionSerializer? serializer = null) =>
        (serializer ?? ExpressionSerializer.Default).ToPredicate<TRoot>(this);

    /// <summary>Rebuilds the lambda letting the body decide the result type.</summary>
    /// <param name="serializer">Serializer to use; <see cref="ExpressionSerializer.Default"/> when omitted.</param>
    public LambdaExpression ToLambda(ExpressionSerializer? serializer = null) =>
        (serializer ?? ExpressionSerializer.Default).ToLambda(this, typeof(TRoot));

    /// <summary>Serializes this model to JSON.</summary>
    /// <param name="serializer">Serializer to use; <see cref="ExpressionSerializer.Default"/> when omitted.</param>
    public string ToJson(ExpressionSerializer? serializer = null) =>
        (serializer ?? ExpressionSerializer.Default).ToJson(this);

    /// <summary>Deserializes a model from JSON.</summary>
    /// <param name="json">JSON payload.</param>
    /// <param name="serializer">Serializer to use; <see cref="ExpressionSerializer.Default"/> when omitted.</param>
    public static ExpressionModel<TRoot> FromJson(string json, ExpressionSerializer? serializer = null) =>
        (serializer ?? ExpressionSerializer.Default).ModelFromJson<TRoot>(json);
}
