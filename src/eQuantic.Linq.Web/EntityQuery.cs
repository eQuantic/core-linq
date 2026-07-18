using System.Globalization;
using System.Linq.Expressions;
using eQuantic.Linq.Expressions;
using eQuantic.Linq.Expressions.Nodes;
using eQuantic.Linq.Web.Syntax;

namespace eQuantic.Linq.Web;

/// <summary>Parses complete query strings (<c>filter</c>, <c>orderBy</c>, <c>skip</c>, <c>take</c>, <c>select</c>) into applicable entity queries.</summary>
public static class EntityQuery
{
    /// <summary>Parses a raw query string (leading <c>?</c> optional, values URL-encoded).</summary>
    /// <typeparam name="T">Root entity type.</typeparam>
    /// <param name="queryString">Raw query string, e.g. <c>?filter=total:gt(100)&amp;orderBy=total:desc&amp;take=10</c>.</param>
    /// <param name="options">Syntax options; defaults apply when omitted.</param>
    public static EntityQuery<T> Parse<T>(string queryString, QueryStringOptions? options = null) =>
        Parse<T>(QueryStringSplitter.Split(queryString), options);

    /// <summary>Parses pre-split query parameters (e.g. from a framework's query collection).</summary>
    /// <typeparam name="T">Root entity type.</typeparam>
    /// <param name="parameters">Key/value pairs; repeated keys are allowed.</param>
    /// <param name="options">Syntax options; defaults apply when omitted.</param>
    public static EntityQuery<T> Parse<T>(
        IEnumerable<KeyValuePair<string, string>> parameters,
        QueryStringOptions? options = null)
    {
        if (parameters is null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        options ??= QueryStringOptions.Default;

        var filterBodies = new List<ExpressionNode>();
        string? orderBy = null;
        string? select = null;
        int? skip = null;
        int? take = null;

        foreach (var pair in parameters)
        {
            if (string.IsNullOrWhiteSpace(pair.Value))
            {
                continue;
            }

            if (Is(pair.Key, options.FilterKey))
            {
                filterBodies.Add(FilterSyntaxParser.ParseBody(pair.Value, options.RootParameterName));
            }
            else if (Is(pair.Key, options.OrderByKey))
            {
                orderBy = orderBy is null ? pair.Value : orderBy + "," + pair.Value;
            }
            else if (Is(pair.Key, options.SkipKey))
            {
                skip = ParseCount(pair.Value, options.SkipKey);
            }
            else if (Is(pair.Key, options.TakeKey))
            {
                take = ParseCount(pair.Value, options.TakeKey);
            }
            else if (Is(pair.Key, options.SelectKey))
            {
                select = pair.Value;
            }
        }

        ExpressionModel<T>? filterModel = null;
        Expression<Func<T, bool>>? filter = null;
        if (filterBodies.Count > 0)
        {
            filterModel = new ExpressionModel<T>
            {
                Parameters = [new ParameterNode { Name = options.RootParameterName }],
                Body = FilterSyntaxParser.Combine(filterBodies, ExpressionType.AndAlso),
            };
            filter = options.Serializer.ToPredicate(filterModel);
        }

        var sorts = orderBy is null
            ? (IReadOnlyList<QuerySort<T>>)[]
            : QuerySort<T>.Parse(orderBy, options);

        var selector = select is null ? null : SelectSyntaxParser.Build(typeof(T), select, options);

        return new EntityQuery<T>(filterModel, filter, sorts, skip, take, selector, options);
    }

    private static bool Is(string key, string expected) =>
        string.Equals(key, expected, StringComparison.OrdinalIgnoreCase);

    private static int ParseCount(string value, string key)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed < 0)
        {
            throw new QueryStringParseException($"'{key}' must be a non-negative integer", 0, value);
        }

        return parsed;
    }
}

/// <summary>A parsed entity query: filter, ordering, paging and projection, applicable to any <see cref="IQueryable{T}"/>.</summary>
/// <typeparam name="T">Root entity type.</typeparam>
public sealed class EntityQuery<T>
{
    private readonly QueryStringOptions _options;

    internal EntityQuery(
        ExpressionModel<T>? filterModel,
        Expression<Func<T, bool>>? filter,
        IReadOnlyList<QuerySort<T>> sorts,
        int? skip,
        int? take,
        LambdaExpression? selector,
        QueryStringOptions options)
    {
        FilterModel = filterModel;
        Filter = filter;
        Sorts = sorts;
        Skip = skip;
        Take = take;
        Selector = selector;
        _options = options;
    }

    /// <summary>The filter as a transport-friendly, JSON-serializable model (null when no filter was supplied).</summary>
    public ExpressionModel<T>? FilterModel { get; }

    /// <summary>The typed filter predicate (null when no filter was supplied).</summary>
    public Expression<Func<T, bool>>? Filter { get; }

    /// <summary>Parsed ordering segments, in precedence order.</summary>
    public IReadOnlyList<QuerySort<T>> Sorts { get; }

    /// <summary>Number of elements to skip, when supplied.</summary>
    public int? Skip { get; }

    /// <summary>Number of elements to take, when supplied.</summary>
    public int? Take { get; }

    /// <summary>Anonymous-type projection selector (null when no <c>select</c> was supplied).</summary>
    public LambdaExpression? Selector { get; }

    /// <summary>Applies filter, ordering and paging to the source.</summary>
    /// <param name="source">Queryable source.</param>
    public IQueryable<T> Apply(IQueryable<T> source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        var query = source;

        if (Filter is not null)
        {
            query = query.Where(Filter);
        }

        query = QueryApplier.ApplySorts(query, Sorts);

        if (Skip is not null)
        {
            query = query.Skip(Skip.Value);
        }

        if (Take is not null)
        {
            query = query.Take(Take.Value);
        }

        return query;
    }

    /// <summary>
    /// Casts this query — authored over the exposed DTO shape — onto the internal entity shape:
    /// filter, sorts and projection are rewritten through the given
    /// <see cref="eQuantic.Linq.Expressions.Casting.ExpressionCast{TSource, TTarget}"/>.
    /// </summary>
    /// <typeparam name="TTarget">Entity type the query will actually run on.</typeparam>
    /// <param name="cast">Reusable cast configuration.</param>
    public EntityQuery<TTarget> Cast<TTarget>(eQuantic.Linq.Expressions.Casting.ExpressionCast<T, TTarget> cast)
    {
        if (cast is null)
        {
            throw new ArgumentNullException(nameof(cast));
        }

        var filter = Filter is null ? null : cast.Predicate(Filter);
        var filterModel = filter is null ? null : _options.Serializer.ToModel(filter);

        IReadOnlyList<QuerySort<TTarget>> sorts = Sorts.Count == 0
            ? []
            : Sorts.Select(s => new QuerySort<TTarget>(s.Path, s.Direction, cast.Lambda(s.KeySelector))).ToList();

        var selector = Selector is null ? null : cast.Lambda(Selector);

        return new EntityQuery<TTarget>(filterModel, filter, sorts, Skip, Take, selector, _options);
    }

    /// <summary>Casts this query onto the entity shape with an inline mapping configuration.</summary>
    /// <typeparam name="TTarget">Entity type the query will actually run on.</typeparam>
    /// <param name="configure">Mapping configuration; by-name auto-mapping applies when omitted.</param>
    public EntityQuery<TTarget> Cast<TTarget>(Action<eQuantic.Linq.Expressions.Casting.CastOptions<T, TTarget>>? configure = null) =>
        Cast(eQuantic.Linq.Expressions.Casting.ExpressionCast.Create(configure));

    /// <summary>Applies the full query including the projection; falls back to <see cref="Apply"/> when no <c>select</c> was supplied.</summary>
    /// <param name="source">Queryable source.</param>
    public IQueryable ApplyWithSelection(IQueryable<T> source)
    {
        var query = Apply(source);

        if (Selector is null)
        {
            return query;
        }

        var call = Expression.Call(
            typeof(Queryable),
            nameof(Queryable.Select),
            [typeof(T), Selector.ReturnType],
            query.Expression,
            Expression.Quote(Selector));

        return query.Provider.CreateQuery(call);
    }
}

/// <summary>Shared dynamic application helpers.</summary>
internal static class QueryApplier
{
    public static IQueryable<T> ApplySorts<T>(IQueryable<T> query, IReadOnlyList<QuerySort<T>> sorts)
    {
        var first = true;

        foreach (var sort in sorts)
        {
            var method = first
                ? (sort.Direction == SortDirection.Ascending ? nameof(Queryable.OrderBy) : nameof(Queryable.OrderByDescending))
                : (sort.Direction == SortDirection.Ascending ? nameof(Queryable.ThenBy) : nameof(Queryable.ThenByDescending));

            var call = Expression.Call(
                typeof(Queryable),
                method,
                [typeof(T), sort.KeySelector.ReturnType],
                query.Expression,
                Expression.Quote(sort.KeySelector));

            query = query.Provider.CreateQuery<T>(call);
            first = false;
        }

        return query;
    }
}
