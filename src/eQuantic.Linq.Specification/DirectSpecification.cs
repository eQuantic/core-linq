using System.Linq.Expressions;

namespace eQuantic.Linq.Specification;

/// <summary>Specification satisfied by a directly supplied predicate expression.</summary>
/// <typeparam name="TEntity">Type of entity that checks this specification.</typeparam>
public sealed class DirectSpecification<TEntity> : Specification<TEntity> where TEntity : class
{
    private readonly Expression<Func<TEntity, bool>> matchingCriteria;

    /// <summary>Creates the specification from a predicate expression.</summary>
    /// <param name="matchingCriteria">Predicate the specification is satisfied by.</param>
    public DirectSpecification(Expression<Func<TEntity, bool>> matchingCriteria)
    {
        this.matchingCriteria = matchingCriteria ?? throw new ArgumentNullException(nameof(matchingCriteria), "No criteria were informed.");
    }

    /// <inheritdoc />
    public override Expression<Func<TEntity, bool>> SatisfiedBy()
    {
        return matchingCriteria;
    }
}
