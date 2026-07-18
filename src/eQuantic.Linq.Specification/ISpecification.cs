using System.Linq.Expressions;

namespace eQuantic.Linq.Specification;

/// <summary>Composable specification over <typeparamref name="T"/> (specification pattern).</summary>
/// <typeparam name="T">Type of entity that checks this specification.</typeparam>
public interface ISpecification<T> where T : class
{
    /// <summary>Combines this specification with another using a logical AND.</summary>
    /// <param name="specification">Right operand.</param>
    ISpecification<T> And(ISpecification<T> specification);

    /// <summary>Combines this specification with the negation of another using a logical AND.</summary>
    /// <param name="specification">Specification to negate on the right side.</param>
    ISpecification<T> AndNot(ISpecification<T> specification);

    /// <summary>Negates this specification.</summary>
    ISpecification<T> Not();

    /// <summary>Combines this specification with another using a logical OR.</summary>
    /// <param name="specification">Right operand.</param>
    ISpecification<T> Or(ISpecification<T> specification);

    /// <summary>Combines this specification with the negation of another using a logical OR.</summary>
    /// <param name="specification">Specification to negate on the right side.</param>
    ISpecification<T> OrNot(ISpecification<T> specification);

    /// <summary>The predicate satisfied by this specification.</summary>
    Expression<Func<T, bool>> SatisfiedBy();
}
