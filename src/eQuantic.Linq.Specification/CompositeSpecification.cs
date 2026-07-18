namespace eQuantic.Linq.Specification;

/// <summary>Base class of specifications composed of two sides.</summary>
/// <typeparam name="TEntity">Type of entity that checks this specification.</typeparam>
public abstract class CompositeSpecification<TEntity> : Specification<TEntity> where TEntity : class
{
    /// <summary>Left side of the composition.</summary>
    public abstract ISpecification<TEntity> LeftSpecification { get; }

    /// <summary>Right side of the composition.</summary>
    public abstract ISpecification<TEntity> RightSpecification { get; }
}
