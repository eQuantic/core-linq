using System.Linq.Expressions;

namespace eQuantic.Linq.Expressions.Casting;

/// <summary>
/// Mapping configuration between a source shape (typically an API DTO) and a target shape
/// (typically the data model). Unmapped members are matched by name (case-insensitive) by default;
/// explicit maps may point at arbitrary target expressions — navigations, arithmetic, concatenation,
/// aggregates.
/// </summary>
/// <typeparam name="TSource">Source (exposed) type.</typeparam>
/// <typeparam name="TTarget">Target (internal) type.</typeparam>
public sealed class CastOptions<TSource, TTarget>
{
    private readonly CastRegistry _registry;
    private readonly CastRegistry.Pair _pair;

    internal CastOptions(CastRegistry registry)
    {
        _registry = registry;
        _pair = registry.GetOrAddPair(typeof(TSource), typeof(TTarget));
    }

    /// <summary>
    /// Whether unmapped source members are matched to target members by name (case-insensitive).
    /// Enabled by default; when disabled, every used member requires an explicit map.
    /// </summary>
    public bool AutoMapByName
    {
        get => _pair.AutoMapByName;
        set => _pair.AutoMapByName = value;
    }

    /// <summary>
    /// Whether a source member's <c>[Column("…")]</c> attribute participates in by-name matching
    /// (the column name is used as an additional lookup key on the target, and target members are
    /// also matched by their own <c>[Column]</c> names). Enabled by default.
    /// </summary>
    public bool ColumnFallback
    {
        get => _pair.ColumnFallback;
        set => _pair.ColumnFallback = value;
    }

    /// <summary>
    /// Maps a source member to a target expression of the same value type — a member path,
    /// arithmetic, concatenation, an aggregate, or any other computable expression.
    /// </summary>
    /// <typeparam name="TValue">Value type (identical on both sides, so rewritten usages stay valid).</typeparam>
    /// <param name="source">Simple member access over the source parameter (e.g. <c>d =&gt; d.CustomerName</c>).</param>
    /// <param name="target">Replacement expression over the target (e.g. <c>e =&gt; e.Customer.Name</c>).</param>
    public CastOptions<TSource, TTarget> Map<TValue>(
        Expression<Func<TSource, TValue>> source,
        Expression<Func<TTarget, TValue>> target)
    {
        _pair.AddMap(ExtractMemberName(source), target);
        return this;
    }

    /// <summary>Maps a source member by name to a target lambda (low-level overload).</summary>
    /// <param name="sourceMemberName">Source member name (case-insensitive).</param>
    /// <param name="target">Replacement lambda; its single parameter must be of type <typeparamref name="TTarget"/>.</param>
    public CastOptions<TSource, TTarget> Map(string sourceMemberName, LambdaExpression target)
    {
        if (string.IsNullOrWhiteSpace(sourceMemberName))
        {
            throw new ArgumentException("Source member name is required.", nameof(sourceMemberName));
        }

        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        if (target.Parameters.Count != 1 || !target.Parameters[0].Type.IsAssignableFrom(typeof(TTarget)))
        {
            throw new ArgumentException(
                $"The target lambda must take a single '{typeof(TTarget)}' parameter.", nameof(target));
        }

        _pair.AddMap(sourceMemberName, target);
        return this;
    }

    /// <summary>
    /// Configures the mapping between nested shapes — navigation targets or collection elements
    /// (e.g. <c>ItemDto</c> ↔ <c>OrderItem</c> for <c>d.Items.Any(i =&gt; …)</c>).
    /// </summary>
    /// <typeparam name="TNestedSource">Nested source type.</typeparam>
    /// <typeparam name="TNestedTarget">Nested target type.</typeparam>
    /// <param name="configure">Optional nested mapping configuration.</param>
    public CastOptions<TSource, TTarget> Nested<TNestedSource, TNestedTarget>(
        Action<CastOptions<TNestedSource, TNestedTarget>>? configure = null)
    {
        var nested = new CastOptions<TNestedSource, TNestedTarget>(_registry);
        configure?.Invoke(nested);
        return this;
    }

    private static string ExtractMemberName(LambdaExpression source)
    {
        var body = source.Body;

        // Strip boxing/widening conversions (e.g. object-typed member selectors).
        while (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
        {
            body = unary.Operand;
        }

        if (body is MemberExpression { Expression: ParameterExpression } member)
        {
            return member.Member.Name;
        }

        throw new ArgumentException(
            "The source selector must be a simple member access over the parameter (e.g. d => d.Name). " +
            "For nested shapes use Nested<TSource, TTarget>(...).",
            nameof(source));
    }
}
