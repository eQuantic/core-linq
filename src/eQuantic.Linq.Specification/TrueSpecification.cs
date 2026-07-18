using System.Linq.Expressions;

namespace eQuantic.Linq.Specification;

/// <summary>Specification satisfied by every entity (identity element for AND compositions).</summary>
/// <typeparam name="TEntity">Type of entity that checks this specification.</typeparam>
public class TrueSpecification<TEntity> : Specification<TEntity> where TEntity : class
{
    /// <inheritdoc />
    public override Expression<Func<TEntity, bool>> SatisfiedBy()
    {
        Expression<Func<TEntity, bool>> trueExpression = t => true;
        return trueExpression;
    }
}
