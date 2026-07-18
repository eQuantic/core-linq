using System.Linq.Expressions;

namespace eQuantic.Linq.Specification;

/// <summary>Composite specification whose sides are provided directly (as specifications or expressions).</summary>
/// <typeparam name="T">Type of entity that checks this specification.</typeparam>
public abstract class DirectCompositeSpecification<T> : CompositeSpecification<T> where T : class
{
    /// <summary>Creates the composition from two specifications.</summary>
    /// <param name="left">Left side specification.</param>
    /// <param name="right">Right side specification.</param>
    protected DirectCompositeSpecification(ISpecification<T> left, ISpecification<T> right)
    {
        LeftSpecification = left ?? throw new ArgumentNullException(nameof(left));
        RightSpecification = right ?? throw new ArgumentNullException(nameof(right));
    }

    /// <summary>Creates the composition from two expressions.</summary>
    /// <param name="left">Left side expression.</param>
    /// <param name="right">Right side expression.</param>
    protected DirectCompositeSpecification(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        LeftSpecification = left != null ? new DirectSpecification<T>(left) : throw new ArgumentNullException(nameof(left));
        RightSpecification = right != null ? new DirectSpecification<T>(right) : throw new ArgumentNullException(nameof(right));
    }

    /// <inheritdoc />
    public override ISpecification<T> LeftSpecification { get; }

    /// <inheritdoc />
    public override ISpecification<T> RightSpecification { get; }
}
