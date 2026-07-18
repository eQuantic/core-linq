using System.Linq.Expressions;
using System.Reflection;
using eQuantic.Linq.Expressions.Metadata;
using eQuantic.Linq.Expressions.Nodes;
using eQuantic.Linq.Expressions.Resolution;

namespace eQuantic.Linq.Expressions;

/// <summary>
/// Member-path utilities: extract dotted paths from selector lambdas (the v2 <c>GetColumnName</c>
/// role, with optional <c>[Column]</c> fallback) and build selector lambdas back from paths through
/// the engine's inference.
/// </summary>
public static class MemberPathExtensions
{
    /// <summary>The member accessed by a simple selector (last segment of the chain).</summary>
    /// <param name="selector">Selector lambda over the parameter (e.g. <c>x =&gt; x.Customer.Name</c>).</param>
    public static MemberInfo GetMember(this LambdaExpression selector) =>
        Last(Segments(selector));

    /// <summary>The accessed member's name, optionally using its <c>[Column("…")]</c> name.</summary>
    /// <param name="selector">Selector lambda.</param>
    /// <param name="columnFallback">Whether a <c>[Column]</c> attribute overrides the CLR name.</param>
    public static string GetMemberName(this LambdaExpression selector, bool columnFallback = false) =>
        Name(Last(Segments(selector)), columnFallback);

    /// <summary>The full dotted path of the selector (e.g. <c>Customer.Name</c>).</summary>
    /// <param name="selector">Selector lambda.</param>
    /// <param name="columnFallback">Whether <c>[Column]</c> attributes override the CLR names.</param>
    public static string GetMemberPath(this LambdaExpression selector, bool columnFallback = false) =>
        string.Join(".", Segments(selector).Select(m => Name(m, columnFallback)));

    /// <summary>
    /// Builds a typed selector lambda from a dotted path (the reverse direction), resolved through
    /// the engine's inference — case-insensitive names and <c>[Column]</c> fallback included.
    /// </summary>
    /// <typeparam name="T">Root type.</typeparam>
    /// <param name="path">Dotted member path (e.g. <c>customer.name</c>).</param>
    /// <param name="serializer">Serializer to use; <see cref="ExpressionSerializer.Default"/> when omitted.</param>
    public static LambdaExpression ToSelector<T>(string path, ExpressionSerializer? serializer = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Member path is required.", nameof(path));
        }

        ExpressionNode body = new ParameterNode { Name = "x" };
        foreach (var segment in path.Split('.'))
        {
            body = new MemberNode { Member = new MemberRef { Name = segment.Trim() }, Expression = body };
        }

        var model = new ExpressionModel
        {
            Parameters = [new ParameterNode { Name = "x" }],
            Body = body,
        };

        return (serializer ?? ExpressionSerializer.Default).ToLambda(model, typeof(T));
    }

    private static MemberInfo Last(List<MemberInfo> segments) => segments[segments.Count - 1];

    private static List<MemberInfo> Segments(LambdaExpression selector)
    {
        if (selector is null)
        {
            throw new ArgumentNullException(nameof(selector));
        }

        var segments = new List<MemberInfo>();
        var current = selector.Body;

        while (current is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
        {
            current = unary.Operand;
        }

        while (current is MemberExpression member)
        {
            segments.Add(member.Member);
            current = member.Expression!;

            while (current is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } inner)
            {
                current = inner.Operand;
            }
        }

        if (current is not ParameterExpression || segments.Count == 0)
        {
            throw new ArgumentException(
                $"The selector must be a member-access chain over the parameter (e.g. x => x.Customer.Name); got '{selector.Body}'.",
                nameof(selector));
        }

        segments.Reverse();
        return segments;
    }

    private static string Name(MemberInfo member, bool columnFallback) =>
        columnFallback && MemberResolver.GetColumnName(member) is { } column ? column : member.Name;
}
