using System.Linq.Expressions;

namespace eQuantic.Linq.Specification;

/// <summary>Base class of composable specifications, with operator overloads (<c>&amp;</c>, <c>|</c>, <c>!</c>).</summary>
/// <typeparam name="T">Type of entity that checks this specification.</typeparam>
public abstract class Specification<T> : ISpecification<T> where T : class
{
    /// <summary>
    /// Not specification
    /// </summary>
    /// <param name="specification">Specification to negate</param>
    /// <returns>New specification</returns>
    public static Specification<T> operator !(Specification<T> specification)
    {
        return new NotSpecification<T>(specification);
    }

    /// <summary>
    ///  Operator And
    /// </summary>
    /// <param name="leftSideSpecification">left operand in this AND operation</param>
    /// <param name="rightSideSpecification">right operand in this AND operation</param>
    /// <returns>New specification</returns>
    public static Specification<T> operator &(Specification<T> leftSideSpecification, Specification<T> rightSideSpecification)
    {
        return new AndSpecification<T>(leftSideSpecification, rightSideSpecification);
    }

    /// <summary>
    /// Or operator
    /// </summary>
    /// <param name="leftSideSpecification">left operand in this OR operation</param>
    /// <param name="rightSideSpecification">left operand in this OR operation</param>
    /// <returns>New specification </returns>
    public static Specification<T> operator |(Specification<T> leftSideSpecification, Specification<T> rightSideSpecification)
    {
        return new OrSpecification<T>(leftSideSpecification, rightSideSpecification);
    }

    /// <summary>
    /// Override operator false, only for support AND OR operators
    /// </summary>
    /// <param name="specification">Specification instance</param>
    /// <returns>See False operator in C#</returns>
    public static bool operator false(Specification<T> specification)
    {
        return false;
    }

    /// <summary>
    /// Override operator True, only for support AND OR operators
    /// </summary>
    /// <param name="specification">Specification instance</param>
    /// <returns>See True operator in C#</returns>
    public static bool operator true(Specification<T> specification)
    {
        return false;
    }

    /// <summary>Combines this specification with another using a bitwise (non-short-circuit) AND.</summary>
    /// <param name="specification">Right operand.</param>
    public ISpecification<T> And(ISpecification<T> specification)
    {
        return new AndSpecification<T>(this, specification);
    }

    /// <summary>Combines this specification with another using a short-circuit AND (<c>&amp;&amp;</c>).</summary>
    /// <param name="specification">Right operand.</param>
    public ISpecification<T> AndAlso(ISpecification<T> specification)
    {
        return new AndAlsoSpecification<T>(this, specification);
    }

    /// <summary>Combines this specification with the negation of another using an AND.</summary>
    /// <param name="specification">Specification to negate on the right side.</param>
    public ISpecification<T> AndNot(ISpecification<T> specification)
    {
        return new AndSpecification<T>(this, new NotSpecification<T>(specification));
    }

    /// <summary>Negates this specification.</summary>
    public ISpecification<T> Not()
    {
        return new NotSpecification<T>(this);
    }

    /// <summary>Combines this specification with another using a bitwise (non-short-circuit) OR.</summary>
    /// <param name="specification">Right operand.</param>
    public ISpecification<T> Or(ISpecification<T> specification)
    {
        return new OrSpecification<T>(this, specification);
    }

    /// <summary>Combines this specification with another using a short-circuit OR (<c>||</c>).</summary>
    /// <param name="specification">Right operand.</param>
    public ISpecification<T> OrElse(ISpecification<T> specification)
    {
        return new OrElseSpecification<T>(this, specification);
    }

    /// <summary>Combines this specification with the negation of another using an OR.</summary>
    /// <param name="specification">Specification to negate on the right side.</param>
    public ISpecification<T> OrNot(ISpecification<T> specification)
    {
        return new OrSpecification<T>(this, new NotSpecification<T>(specification));
    }

    /// <summary>The predicate satisfied by this specification.</summary>
    public abstract Expression<Func<T, bool>> SatisfiedBy();
}
