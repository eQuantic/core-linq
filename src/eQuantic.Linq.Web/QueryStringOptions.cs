using System.Collections.Concurrent;
using System.Linq.Expressions;
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

    /// <summary>Null-guard injection policy for applied filters. Defaults to <see cref="NullGuardMode.Auto"/>.</summary>
    public NullGuardMode NullGuards { get; set; } = NullGuardMode.Auto;

    /// <summary>
    /// Caches parsed filter predicates per options instance (APIs receive the same filters over and
    /// over; expressions are immutable and safely reusable). Enabled by default.
    /// </summary>
    public bool CacheParsedFilters { get; set; } = true;

    private const int FilterCacheCapacity = 512;
    private readonly ConcurrentDictionary<(Type Root, string Filter), LambdaExpression> _filterCache = new();

    internal Expression<Func<T, bool>> GetOrAddFilter<T>(string filter, Func<string, Expression<Func<T, bool>>> factory)
    {
        if (!CacheParsedFilters)
        {
            return factory(filter);
        }

        if (_filterCache.Count >= FilterCacheCapacity)
        {
            _filterCache.Clear();
        }

        return (Expression<Func<T, bool>>)_filterCache.GetOrAdd((typeof(T), filter), _ => factory(filter));
    }
}
