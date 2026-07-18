using System.Linq.Expressions;
using eQuantic.Linq.Expressions.Comparison;
using eQuantic.Linq.Expressions.Tests.TestModel;

namespace eQuantic.Linq.Expressions.Tests;

[TestFixture]
public class EqualityComparerTests
{
    private static readonly ExpressionEqualityComparer Comparer = ExpressionEqualityComparer.Instance;

    [Test]
    public void Identical_trees_with_distinct_parameter_instances_are_equal()
    {
        Expression<Func<Order, bool>> first = o => o.Total > 100m && o.Status == OrderStatus.Paid;
        Expression<Func<Order, bool>> second = o => o.Total > 100m && o.Status == OrderStatus.Paid;

        Assert.That(Comparer.Equals(first, second), Is.True);
        Assert.That(Comparer.GetHashCode(first), Is.EqualTo(Comparer.GetHashCode(second)));
    }

    [Test]
    public void Alpha_equivalence_parameter_names_do_not_matter()
    {
        Expression<Func<int, int>> first = x => x + 1;
        Expression<Func<int, int>> second = y => y + 1;

        Assert.That(Comparer.Equals(first, second), Is.True);
    }

    [Test]
    public void Different_constants_are_not_equal()
    {
        Expression<Func<int, bool>> first = x => x > 1;
        Expression<Func<int, bool>> second = x => x > 2;

        Assert.That(Comparer.Equals(first, second), Is.False);
    }

    [Test]
    public void Different_members_are_not_equal()
    {
        Expression<Func<Order, decimal>> first = o => o.Total;
        Expression<Func<Order, decimal?>> second = o => o.Discount;

        Assert.That(Comparer.Equals(first, second), Is.False);
    }

    [Test]
    public void Different_operators_are_not_equal()
    {
        Expression<Func<int, int, int>> first = (a, b) => a + b;
        Expression<Func<int, int, int>> second = (a, b) => a - b;

        Assert.That(Comparer.Equals(first, second), Is.False);
    }

    [Test]
    public void Complex_constant_values_compare_structurally()
    {
        var left = Expression.Constant(new OrderItem { Id = 1, Product = "A", Price = 2m, Quantity = 3 });
        var right = Expression.Constant(new OrderItem { Id = 1, Product = "A", Price = 2m, Quantity = 3 });

        Assert.That(Comparer.Equals(left, right), Is.True);
    }

    [Test]
    public void Crossed_parameter_bindings_are_not_equal()
    {
        var x = Expression.Parameter(typeof(int), "x");
        var y = Expression.Parameter(typeof(int), "y");

        var straight = Expression.Lambda<Func<int, int, int>>(Expression.Subtract(x, y), x, y);
        var crossed = Expression.Lambda<Func<int, int, int>>(Expression.Subtract(y, x), x, y);

        Assert.That(Comparer.Equals(straight, crossed), Is.False);
    }
}
