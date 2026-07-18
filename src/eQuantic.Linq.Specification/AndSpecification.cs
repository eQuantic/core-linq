using System.Linq.Expressions;

namespace eQuantic.Linq.Specification;

/// <summary>A logic AND specification (bitwise, non-short-circuit — see <see cref="AndAlsoSpecification{T}"/> for <c>&amp;&amp;</c>).</summary>
/// <typeparam name="T">Type of entity that checks this specification.</typeparam>
public class AndSpecification<T> : DirectCompositeSpecification<T> where T : class
{
    /// <summary>Creates the AND composition from two specifications.</summary>
    /// <param name="left">Left side specification.</param>
    /// <param name="right">Right side specification.</param>
    public AndSpecification(ISpecification<T> left, ISpecification<T> right) : base(left, right)
    {
    }

    /// <summary>Creates the AND composition from two expressions.</summary>
    /// <param name="left">Left side expression.</param>
    /// <param name="right">Right side expression.</param>
    public AndSpecification(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right) : base(left, right)
    {
    }

    /// <inheritdoc />
    public override Expression<Func<T, bool>> SatisfiedBy()
    {
        var left = LeftSpecification.SatisfiedBy();
        var right = RightSpecification.SatisfiedBy();

        return left.And(right);
    }
}
