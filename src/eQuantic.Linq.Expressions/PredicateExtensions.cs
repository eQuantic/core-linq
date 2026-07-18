using System.Linq.Expressions;
using eQuantic.Linq.Expressions.Casting;

namespace eQuantic.Linq.Expressions;

/// <summary>Seeds for dynamically composed predicates.</summary>
public static class PredicateBuilder
{
    /// <summary>A predicate satisfied by every element (identity for AND chains).</summary>
    public static Expression<Func<T, bool>> True<T>() => _ => true;

    /// <summary>A predicate satisfied by no element (identity for OR chains).</summary>
    public static Expression<Func<T, bool>> False<T>() => _ => false;
}

/// <summary>
/// Provider-friendly composition over <see cref="Expression{TDelegate}"/> predicates: parameters are
/// rebound onto a single instance (no <c>Invoke</c>), so composed trees stay translatable.
/// </summary>
public static class PredicateExtensions
{
    /// <summary>Combines two predicates with a bitwise (non-short-circuit) AND.</summary>
    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second) =>
        first.Compose(second, Expression.And);

    /// <summary>Combines two predicates with a short-circuit AND (<c>&amp;&amp;</c>).</summary>
    public static Expression<Func<T, bool>> AndAlso<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second) =>
        first.Compose(second, Expression.AndAlso);

    /// <summary>Combines two predicates with a bitwise (non-short-circuit) OR.</summary>
    public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second) =>
        first.Compose(second, Expression.Or);

    /// <summary>Combines two predicates with a short-circuit OR (<c>||</c>).</summary>
    public static Expression<Func<T, bool>> OrElse<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second) =>
        first.Compose(second, Expression.OrElse);

    /// <summary>Negates a predicate.</summary>
    public static Expression<Func<T, bool>> Not<T>(this Expression<Func<T, bool>> predicate) =>
        Expression.Lambda<Func<T, bool>>(Expression.Not(predicate.Body), predicate.Parameters);

    /// <summary>Applies C# <c>?.</c>-style null propagation for in-memory execution (see <see cref="NullGuards"/>).</summary>
    public static Expression<Func<T, bool>> WithNullGuards<T>(this Expression<Func<T, bool>> predicate) =>
        NullGuards.Apply(predicate);

    /// <summary>Merges two lambdas with a custom combiner, rebinding the second lambda's parameters onto the first's.</summary>
    /// <typeparam name="T">Delegate type.</typeparam>
    /// <param name="first">Lambda providing the surviving parameters.</param>
    /// <param name="second">Lambda whose parameters are rebound.</param>
    /// <param name="merge">Body combiner (e.g. <see cref="Expression.AndAlso(Expression, Expression)"/>).</param>
    public static Expression<T> Compose<T>(this Expression<T> first, Expression<T> second, Func<Expression, Expression, Expression> merge)
    {
        var secondBody = second.Body;
        for (var i = 0; i < first.Parameters.Count; i++)
        {
            secondBody = ParameterReplacer.Replace(secondBody, second.Parameters[i], first.Parameters[i]);
        }

        return Expression.Lambda<T>(merge(first.Body, secondBody), first.Parameters);
    }
}
