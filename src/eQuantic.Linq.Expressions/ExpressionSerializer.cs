using System.Linq.Expressions;
using System.Text.Json;
using eQuantic.Linq.Expressions.Conversion;
using eQuantic.Linq.Expressions.Nodes;
using eQuantic.Linq.Expressions.Serialization;

namespace eQuantic.Linq.Expressions;

/// <summary>
/// Converts any .NET expression tree into a fully structured, serializable model
/// (<see cref="ExpressionNode"/>) or its JSON form, and rebuilds the exact expression back.
/// Thread-safe; reuse instances.
/// </summary>
public sealed class ExpressionSerializer
{
    /// <summary>Shared serializer with default options.</summary>
    public static ExpressionSerializer Default { get; } = new();

    /// <summary>Creates a serializer.</summary>
    /// <param name="options">Behavioral options; defaults apply when omitted.</param>
    public ExpressionSerializer(ExpressionSerializerOptions? options = null)
    {
        Options = options ?? new ExpressionSerializerOptions();
        JsonOptions = ExpressionJson.CreateOptions(Options);
    }

    /// <summary>Options in effect for this serializer.</summary>
    public ExpressionSerializerOptions Options { get; }

    /// <summary>JSON options used for the model payloads (exposed for advanced integration).</summary>
    public JsonSerializerOptions JsonOptions { get; }

    /// <summary>Converts an expression into the structured node model.</summary>
    /// <param name="expression">Expression to convert.</param>
    public ExpressionNode ToNode(Expression expression)
    {
        if (expression is null)
        {
            throw new ArgumentNullException(nameof(expression));
        }

        var prepared = Options.EnablePartialEvaluation ? PartialEvaluator.Eval(expression) : expression;
        return new ExpressionToNodeConverter(Options).Convert(prepared);
    }

    /// <summary>Rebuilds the expression represented by a node model.</summary>
    /// <param name="node">Model to convert back.</param>
    public Expression ToExpression(ExpressionNode node)
    {
        if (node is null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        return new NodeToExpressionConverter(Options, JsonOptions).Convert(node);
    }

    /// <summary>Rebuilds a strongly typed lambda from a node model.</summary>
    /// <typeparam name="TDelegate">Expected delegate type.</typeparam>
    /// <param name="node">Model to convert back.</param>
    public Expression<TDelegate> ToExpression<TDelegate>(ExpressionNode node)
    {
        var expression = ToExpression(node);

        if (expression is Expression<TDelegate> typed)
        {
            return typed;
        }

        if (expression is LambdaExpression lambda)
        {
            return Expression.Lambda<TDelegate>(lambda.Body, lambda.Name, lambda.TailCall, lambda.Parameters);
        }

        throw new ExpressionSerializationException(
            $"The node rebuilt to '{expression.NodeType}' which is not a lambda compatible with '{typeof(TDelegate)}'.");
    }

    /// <summary>Serializes an expression to its JSON model.</summary>
    /// <param name="expression">Expression to serialize.</param>
    public string ToJson(Expression expression) => ToJson(ToNode(expression));

    /// <summary>Serializes a node model to JSON.</summary>
    /// <param name="node">Model to serialize.</param>
    public string ToJson(ExpressionNode node)
    {
        if (node is null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        return JsonSerializer.Serialize(node, JsonOptions);
    }

    /// <summary>Deserializes a JSON payload into the node model.</summary>
    /// <param name="json">JSON payload.</param>
    public ExpressionNode NodeFromJson(string json)
    {
        if (json is null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        return JsonSerializer.Deserialize<ExpressionNode>(json, JsonOptions)
               ?? throw new ExpressionSerializationException("The JSON payload deserialized to null.");
    }

    /// <summary>Deserializes a JSON payload and rebuilds the expression.</summary>
    /// <param name="json">JSON payload.</param>
    public Expression FromJson(string json) => ToExpression(NodeFromJson(json));

    /// <summary>Deserializes a JSON payload and rebuilds a strongly typed lambda.</summary>
    /// <typeparam name="TDelegate">Expected delegate type.</typeparam>
    /// <param name="json">JSON payload.</param>
    public Expression<TDelegate> FromJson<TDelegate>(string json) => ToExpression<TDelegate>(NodeFromJson(json));

    // ---------------------------------------------------------------- root-anchored models

    /// <summary>
    /// Converts a lambda into a root-anchored model. With <see cref="TypeInfoMode.Minimal"/> (the default)
    /// every piece of type information that can be re-inferred from <typeparamref name="TRoot"/> is omitted,
    /// producing a lean, hand-writable payload.
    /// </summary>
    /// <typeparam name="TRoot">Root entity of the lambda's first parameter.</typeparam>
    /// <typeparam name="TResult">Lambda result type.</typeparam>
    /// <param name="expression">Expression to convert.</param>
    /// <param name="typeInfo">How much type information to embed.</param>
    public ExpressionModel<TRoot> ToModel<TRoot, TResult>(
        Expression<Func<TRoot, TResult>> expression,
        TypeInfoMode typeInfo = TypeInfoMode.Minimal)
    {
        if (expression is null)
        {
            throw new ArgumentNullException(nameof(expression));
        }

        var model = new ExpressionModel<TRoot>();
        FillModel(model, expression, typeof(TRoot), typeInfo);
        return model;
    }

    /// <summary>Converts a lambda into a root-anchored model (untyped overload).</summary>
    /// <param name="lambda">Lambda to convert; its first parameter must be assignable from <paramref name="rootType"/>.</param>
    /// <param name="rootType">Root entity type used as the inference anchor.</param>
    /// <param name="typeInfo">How much type information to embed.</param>
    public ExpressionModel ToModel(LambdaExpression lambda, Type rootType, TypeInfoMode typeInfo = TypeInfoMode.Minimal)
    {
        if (lambda is null)
        {
            throw new ArgumentNullException(nameof(lambda));
        }

        var model = new ExpressionModel();
        FillModel(model, lambda, rootType, typeInfo);
        return model;
    }

    private void FillModel(ExpressionModel model, LambdaExpression lambda, Type rootType, TypeInfoMode typeInfo)
    {
        if (lambda.Parameters.Count == 0)
        {
            throw new ExpressionSerializationException("Root-anchored models require at least one lambda parameter.");
        }

        var prepared = Options.EnablePartialEvaluation
            ? (LambdaExpression)PartialEvaluator.Eval(lambda)
            : lambda;

        new ExpressionToNodeConverter(Options, typeInfo).FillModel(model, prepared, rootType);
    }

    /// <summary>Rebuilds the lambda described by a root-anchored model.</summary>
    /// <param name="model">Model to rebuild.</param>
    /// <param name="rootType">Root entity type anchoring the first parameter.</param>
    public LambdaExpression ToLambda(ExpressionModel model, Type rootType)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        return new NodeToExpressionConverter(Options, JsonOptions).ConvertModel(model, rootType);
    }

    /// <summary>Rebuilds a strongly typed lambda from a root-anchored model.</summary>
    /// <typeparam name="TRoot">Root entity type.</typeparam>
    /// <typeparam name="TResult">Expected result type.</typeparam>
    /// <param name="model">Model to rebuild.</param>
    public Expression<Func<TRoot, TResult>> ToExpression<TRoot, TResult>(ExpressionModel<TRoot> model)
    {
        var lambda = ToLambda(model, typeof(TRoot));

        if (lambda is Expression<Func<TRoot, TResult>> typed)
        {
            return typed;
        }

        if (lambda.Parameters.Count != 1)
        {
            throw new ExpressionSerializationException(
                $"The model declares {lambda.Parameters.Count} parameters; expected a single '{typeof(TRoot)}' parameter.");
        }

        var body = lambda.Body;
        if (body.Type != typeof(TResult) && typeof(TResult).IsAssignableFrom(body.Type))
        {
            body = Expression.Convert(body, typeof(TResult));
        }

        return Expression.Lambda<Func<TRoot, TResult>>(body, lambda.Name, lambda.TailCall, lambda.Parameters);
    }

    /// <summary>Rebuilds a predicate (<c>TRoot → bool</c>) from a root-anchored model — the <c>Where</c>-filter shape.</summary>
    /// <typeparam name="TRoot">Root entity type.</typeparam>
    /// <param name="model">Model to rebuild.</param>
    public Expression<Func<TRoot, bool>> ToPredicate<TRoot>(ExpressionModel<TRoot> model) =>
        ToExpression<TRoot, bool>(model);

    /// <summary>Serializes a root-anchored model to JSON.</summary>
    /// <param name="model">Model to serialize.</param>
    public string ToJson(ExpressionModel model)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        return JsonSerializer.Serialize(model, model.GetType(), JsonOptions);
    }

    /// <summary>Deserializes a root-anchored model from JSON.</summary>
    /// <typeparam name="TRoot">Root entity type.</typeparam>
    /// <param name="json">JSON payload.</param>
    public ExpressionModel<TRoot> ModelFromJson<TRoot>(string json)
    {
        if (json is null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        return JsonSerializer.Deserialize<ExpressionModel<TRoot>>(json, JsonOptions)
               ?? throw new ExpressionSerializationException("The JSON payload deserialized to null.");
    }
}
