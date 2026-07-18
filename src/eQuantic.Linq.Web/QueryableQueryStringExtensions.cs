namespace eQuantic.Linq.Web;

/// <summary>Query-string application extensions over <see cref="IQueryable{T}"/>.</summary>
public static class QueryableQueryStringExtensions
{
    /// <summary>Filters the source with a query-string filter expression.</summary>
    /// <typeparam name="T">Element type.</typeparam>
    /// <param name="source">Queryable source.</param>
    /// <param name="filter">Filter expression, e.g. <c>total:gt(100),items:any(price:gt(50))</c>.</param>
    /// <param name="options">Syntax options; defaults apply when omitted.</param>
    public static IQueryable<T> WhereQueryString<T>(this IQueryable<T> source, string filter, QueryStringOptions? options = null) =>
        source.Where(QueryFilter.Parse<T>(filter, options));

    /// <summary>Orders the source with a query-string ordering expression.</summary>
    /// <typeparam name="T">Element type.</typeparam>
    /// <param name="source">Queryable source.</param>
    /// <param name="orderBy">Ordering expression, e.g. <c>total:desc,customer.name</c>.</param>
    /// <param name="options">Syntax options; defaults apply when omitted.</param>
    public static IQueryable<T> OrderByQueryString<T>(this IQueryable<T> source, string orderBy, QueryStringOptions? options = null) =>
        QueryApplier.ApplySorts(source, QuerySort<T>.Parse(orderBy, options));

    /// <summary>Applies a full query string (filter, ordering, paging) to the source.</summary>
    /// <typeparam name="T">Element type.</typeparam>
    /// <param name="source">Queryable source.</param>
    /// <param name="queryString">Raw query string, e.g. <c>?filter=…&amp;orderBy=…&amp;skip=0&amp;take=10</c>.</param>
    /// <param name="options">Syntax options; defaults apply when omitted.</param>
    public static IQueryable<T> ApplyQueryString<T>(this IQueryable<T> source, string queryString, QueryStringOptions? options = null) =>
        EntityQuery.Parse<T>(queryString, options).Apply(source);

    /// <summary>Projects the source with a query-string <c>select</c> expression (anonymous-type projection).</summary>
    /// <typeparam name="T">Element type.</typeparam>
    /// <param name="source">Queryable source.</param>
    /// <param name="select">Projection expression, e.g. <c>id,customerName=customer.name,items.count()</c>.</param>
    /// <param name="options">Syntax options; defaults apply when omitted.</param>
    public static IQueryable SelectQueryString<T>(this IQueryable<T> source, string select, QueryStringOptions? options = null)
    {
        options ??= QueryStringOptions.Default;
        var selector = Syntax.SelectSyntaxParser.Build(typeof(T), select, options);

        var call = System.Linq.Expressions.Expression.Call(
            typeof(Queryable),
            nameof(Queryable.Select),
            [typeof(T), selector.ReturnType],
            source.Expression,
            System.Linq.Expressions.Expression.Quote(selector));

        return source.Provider.CreateQuery(call);
    }
}
