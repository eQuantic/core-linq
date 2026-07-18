using System.Linq.Expressions;

namespace eQuantic.Linq.Expressions.Casting;

/// <summary>Factory for <see cref="ExpressionCast{TSource, TTarget}"/> instances.</summary>
#if NET8_0_OR_GREATER
[System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Expression casting re-binds generic methods and re-emits anonymous types at runtime; it is not compatible with Native AOT.")]
#endif
public static class ExpressionCast
{
    /// <summary>
    /// Creates a reusable, thread-safe cast between a source shape (typically an API DTO) and a
    /// target shape (typically the data model).
    /// </summary>
    /// <typeparam name="TSource">Source (exposed) type.</typeparam>
    /// <typeparam name="TTarget">Target (internal) type.</typeparam>
    /// <param name="configure">Mapping configuration; by-name auto-mapping applies when omitted.</param>
    public static ExpressionCast<TSource, TTarget> Create<TSource, TTarget>(
        Action<CastOptions<TSource, TTarget>>? configure = null)
    {
        var registry = new CastRegistry();
        var options = new CastOptions<TSource, TTarget>(registry);
        configure?.Invoke(options);
        return new ExpressionCast<TSource, TTarget>(registry);
    }
}

/// <summary>
/// Rewrites expressions authored over <typeparamref name="TSource"/> (the shape API consumers know)
/// into equivalent expressions over <typeparamref name="TTarget"/> (the shape queries actually run on) —
/// including computed maps (arithmetic, concatenation, aggregates), nested shapes and column fallback.
/// </summary>
/// <typeparam name="TSource">Source (exposed) type.</typeparam>
/// <typeparam name="TTarget">Target (internal) type.</typeparam>
public sealed class ExpressionCast<TSource, TTarget>
{
    private readonly CastRegistry _registry;

    internal ExpressionCast(CastRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>Casts a predicate (<c>Where</c> shape).</summary>
    /// <param name="predicate">Predicate over the source shape.</param>
    public Expression<Func<TTarget, bool>> Predicate(Expression<Func<TSource, bool>> predicate) =>
        (Expression<Func<TTarget, bool>>)Lambda(predicate);

    /// <summary>Casts a selector whose result type is preserved by the configured maps.</summary>
    /// <typeparam name="TResult">Selector result type.</typeparam>
    /// <param name="selector">Selector over the source shape.</param>
    public Expression<Func<TTarget, TResult>> Selector<TResult>(Expression<Func<TSource, TResult>> selector) =>
        (Expression<Func<TTarget, TResult>>)Lambda(selector);

    /// <summary>Casts any lambda (sort key selectors, anonymous projections, …). The result delegate type is re-inferred.</summary>
    /// <param name="lambda">Lambda whose first parameter is of type <typeparamref name="TSource"/>.</param>
    public LambdaExpression Lambda(LambdaExpression lambda)
    {
        if (lambda is null)
        {
            throw new ArgumentNullException(nameof(lambda));
        }

        return new CastRewriter(_registry).Rewrite(lambda, typeof(TSource), typeof(TTarget));
    }

    private LambdaExpression? _projection;

    /// <summary>
    /// Reverse direction: builds the materializer <c>TTarget → TSource</c> (entity → DTO) from the
    /// same mappings — explicit maps are inlined, matching members (including column fallback) are
    /// copied, nested registered collection pairs project element-wise, and members with no
    /// counterpart are skipped.
    /// </summary>
    public Expression<Func<TTarget, TSource>> Project()
    {
        _projection ??= ProjectionBuilder.Build(typeof(TSource), typeof(TTarget), _registry);
        return (Expression<Func<TTarget, TSource>>)_projection;
    }

    /// <summary>Casts a serializable model over the source shape into a model over the target shape.</summary>
    /// <param name="model">Root-anchored model over <typeparamref name="TSource"/>.</param>
    /// <param name="serializer">Serializer used to decode/encode; <see cref="ExpressionSerializer.Default"/> when omitted.</param>
    public ExpressionModel<TTarget> Model(ExpressionModel<TSource> model, ExpressionSerializer? serializer = null)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        serializer ??= ExpressionSerializer.Default;

        var source = serializer.ToLambda(model, typeof(TSource));
        var rewritten = Lambda(source);
        var encoded = serializer.ToModel(rewritten, typeof(TTarget));

        return new ExpressionModel<TTarget>
        {
            Parameters = encoded.Parameters,
            Body = encoded.Body,
            ResultType = encoded.ResultType,
        };
    }
}
