using System.Linq.Expressions;

namespace eQuantic.Linq.Expressions;

/// <summary>
/// Applies serialized expression payloads and member paths directly to <see cref="IQueryable{T}"/> —
/// the engine-level application surface (no web dependency).
/// </summary>
public static class QueryableModelExtensions
{
    /// <summary>Filters the source with a serialized expression model.</summary>
    /// <typeparam name="T">Element type.</typeparam>
    /// <param name="source">Queryable source.</param>
    /// <param name="filter">Root-anchored boolean model.</param>
    /// <param name="serializer">Serializer to use; <see cref="ExpressionSerializer.Default"/> when omitted.</param>
    public static IQueryable<T> Where<T>(this IQueryable<T> source, ExpressionModel<T> filter, ExpressionSerializer? serializer = null) =>
        source.Where((serializer ?? ExpressionSerializer.Default).ToPredicate(filter));

    /// <summary>Filters the source with a serialized expression model received as raw JSON.</summary>
    /// <typeparam name="T">Element type.</typeparam>
    /// <param name="source">Queryable source.</param>
    /// <param name="filterJson">JSON of a root-anchored boolean model.</param>
    /// <param name="serializer">Serializer to use; <see cref="ExpressionSerializer.Default"/> when omitted.</param>
    public static IQueryable<T> WhereJson<T>(this IQueryable<T> source, string filterJson, ExpressionSerializer? serializer = null)
    {
        var effective = serializer ?? ExpressionSerializer.Default;
        return source.Where(effective.ToPredicate(effective.ModelFromJson<T>(filterJson)));
    }

    /// <summary>Orders the source by a dotted member path (e.g. <c>customer.name</c>).</summary>
    /// <typeparam name="T">Element type.</typeparam>
    /// <param name="source">Queryable source.</param>
    /// <param name="path">Dotted member path, resolved through the engine's inference.</param>
    /// <param name="descending">Whether to sort descending.</param>
    /// <param name="serializer">Serializer to use; <see cref="ExpressionSerializer.Default"/> when omitted.</param>
    public static IOrderedQueryable<T> OrderByPath<T>(this IQueryable<T> source, string path, bool descending = false, ExpressionSerializer? serializer = null) =>
        ApplySort(source, path, descending ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy), serializer);

    /// <summary>Adds a subsequent ordering by a dotted member path.</summary>
    /// <typeparam name="T">Element type.</typeparam>
    /// <param name="source">Ordered queryable source.</param>
    /// <param name="path">Dotted member path.</param>
    /// <param name="descending">Whether to sort descending.</param>
    /// <param name="serializer">Serializer to use; <see cref="ExpressionSerializer.Default"/> when omitted.</param>
    public static IOrderedQueryable<T> ThenByPath<T>(this IOrderedQueryable<T> source, string path, bool descending = false, ExpressionSerializer? serializer = null) =>
        ApplySort(source, path, descending ? nameof(Queryable.ThenByDescending) : nameof(Queryable.ThenBy), serializer);

    private static IOrderedQueryable<T> ApplySort<T>(IQueryable<T> source, string path, string method, ExpressionSerializer? serializer)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        var keySelector = MemberPathExtensions.ToSelector<T>(path, serializer);

        var call = Expression.Call(
            typeof(Queryable),
            method,
            [typeof(T), keySelector.ReturnType],
            source.Expression,
            Expression.Quote(keySelector));

        return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(call);
    }
}
