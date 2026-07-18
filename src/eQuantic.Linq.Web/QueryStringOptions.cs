using eQuantic.Linq.Expressions;

namespace eQuantic.Linq.Web;

/// <summary>Configuration of the query-string syntax layer.</summary>
public sealed class QueryStringOptions
{
    internal static readonly QueryStringOptions Default = new();

    /// <summary>Query-string key holding filter expressions. Defaults to <c>filter</c>.</summary>
    public string FilterKey { get; set; } = "filter";

    /// <summary>Query-string key holding ordering expressions. Defaults to <c>orderBy</c>.</summary>
    public string OrderByKey { get; set; } = "orderBy";

    /// <summary>Query-string key holding the number of elements to skip. Defaults to <c>skip</c>.</summary>
    public string SkipKey { get; set; } = "skip";

    /// <summary>Query-string key holding the number of elements to take. Defaults to <c>take</c>.</summary>
    public string TakeKey { get; set; } = "take";

    /// <summary>Query-string key holding projection paths. Defaults to <c>select</c>.</summary>
    public string SelectKey { get; set; } = "select";

    /// <summary>Name given to the root lambda parameter. Defaults to <c>x</c>.</summary>
    public string RootParameterName { get; set; } = "x";

    /// <summary>
    /// Serializer used to materialize parsed models into expressions. Supply a customized instance
    /// to harden type resolution (strict mode) or tune inference. Defaults to <see cref="ExpressionSerializer.Default"/>.
    /// </summary>
    public ExpressionSerializer Serializer { get; set; } = ExpressionSerializer.Default;

    /// <summary>
    /// Hardens these options for untrusted callers: swaps the serializer for
    /// <see cref="ExpressionSerializer.CreateSecure(Type[])"/> restricted to the given contracts.
    /// </summary>
    /// <param name="knownTypes">Contract types query strings may reference.</param>
    public QueryStringOptions UseStrictSerializer(params Type[] knownTypes)
    {
        Serializer = ExpressionSerializer.CreateSecure(knownTypes);
        return this;
    }
}
