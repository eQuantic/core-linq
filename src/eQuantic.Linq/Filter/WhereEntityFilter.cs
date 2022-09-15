using System.Diagnostics;
using System.Linq.Expressions;
using eQuantic.Linq.Specification;

namespace eQuantic.Linq.Filter;

/// <summary>
/// Filters the collection using a predicate.
/// </summary>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
[DebuggerDisplay("EntityFilter ( where {ToString()} )")]
internal sealed class WhereEntityFilter<TEntity> : IEntityFilter<TEntity>
{
    private readonly IEntityFilter<TEntity>? baseFilter;
    private readonly CompositeOperator compositeOperator = CompositeOperator.And;
    private readonly Expression<Func<TEntity, bool>> predicate;

    /// <summary>Initializes a new instance of the <see cref="WhereEntityFilter{TEntity}"/> class.</summary>
    /// <param name="predicate">The predicate.</param>
    public WhereEntityFilter(Expression<Func<TEntity, bool>> predicate)
    {
        this.predicate = predicate;
    }

    /// <summary>Initializes a new instance of the <see cref="WhereEntityFilter{TEntity}"/> class.</summary>
    /// <param name="baseFilter">The base filter.</param>
    /// <param name="predicate">The predicate.</param>
    /// <param name="compositeOperator">The composite operator</param>
    public WhereEntityFilter(IEntityFilter<TEntity> baseFilter, Expression<Func<TEntity, bool>> predicate,
        CompositeOperator compositeOperator = CompositeOperator.And)
    {
        this.baseFilter = baseFilter;
        this.predicate = predicate;
        this.compositeOperator = compositeOperator;
    }

    public override bool Equals(object obj)
    {
        if (obj is not WhereEntityFilter<TEntity> filter)
        {
            return false;
        }

        return baseFilter == filter.baseFilter && predicate.ToString() == filter.predicate.ToString();
    }

    /// <summary>Filters the specified collection.</summary>
    /// <param name="collection">The collection.</param>
    /// <returns>A filtered collection.</returns>
    public IQueryable<TEntity> Filter(IQueryable<TEntity> collection)
    {
        return baseFilter == null ? collection.Where(predicate) : baseFilter.Filter(collection).Where(predicate);
    }

    /// <summary>
    /// Gets the expression.
    /// </summary>
    /// <returns></returns>
    public Expression<Func<TEntity, bool>> GetExpression()
    {
        if (baseFilter == null)
        {
            return predicate;
        }

        return compositeOperator == CompositeOperator.And
            ? baseFilter.GetExpression().AndAlso(predicate)
            : baseFilter.GetExpression().OrElse(predicate);
    }

    public override int GetHashCode()
    {
        return (baseFilter, predicate).GetHashCode();
    }

    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        var baseFilterPresentation =
            baseFilter != null ? baseFilter.ToString() : string.Empty;

        // The returned string is used in de DebuggerDisplay.
        if (!string.IsNullOrEmpty(baseFilterPresentation))
        {
            return baseFilterPresentation + ", " + predicate.ToString();
        }

        return predicate.ToString();
    }
}