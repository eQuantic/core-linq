using System.Linq.Expressions;

namespace eQuantic.Linq.Specification;

/// <summary>
/// A Logic OR Specification
/// </summary>
/// <typeparam name="T">Type of entity that check this specification</typeparam>
public sealed class OrSpecification<T> : DirectCompositeSpecification<T> where T : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrSpecification{T}"/> class.
    /// </summary>
    /// <param name="left">Left side specification</param>
    /// <param name="right">Right side specification</param>
    public OrSpecification(ISpecification<T> left, ISpecification<T> right) : base(left, right)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrSpecification{T}"/> class.
    /// </summary>
    /// <param name="left">Left side expression.</param>
    /// <param name="right">Right side expression.</param>
    public OrSpecification(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right) : base(left, right)
    {
    }

    /// <summary>
    /// <see cref="ISpecification{T}"/>
    /// </summary>
    /// <returns><see cref="ISpecification{T}"/></returns>
    public override Expression<Func<T, bool>> SatisfiedBy()
    {
        var left = LeftSpecification.SatisfiedBy();
        var right = RightSpecification.SatisfiedBy();

        return left.Or(right);
    }
}