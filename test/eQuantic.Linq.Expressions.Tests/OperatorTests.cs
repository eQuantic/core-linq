using System.Linq.Expressions;
using eQuantic.Linq.Expressions.Tests.Support;
using eQuantic.Linq.Expressions.Tests.TestModel;
using static eQuantic.Linq.Expressions.Tests.Support.Verify;

namespace eQuantic.Linq.Expressions.Tests;

[TestFixture]
public class OperatorTests
{
    [Test]
    public void Arithmetic_operators()
    {
        Executes((Expression<Func<int, int, int>>)((x, y) => x + y), Args(3, 4), Args(-5, 5));
        Executes((Expression<Func<int, int, int>>)((x, y) => x - y), Args(10, 4));
        Executes((Expression<Func<int, int, int>>)((x, y) => x * y), Args(6, 7));
        Executes((Expression<Func<int, int, int>>)((x, y) => x / y), Args(42, 5), Args(7, 0));
        Executes((Expression<Func<int, int, int>>)((x, y) => x % y), Args(42, 5));
        Executes((Expression<Func<double, double, double>>)((x, y) => x / y), Args(1.0, 3.0), Args(1.0, 0.0));
        Executes((Expression<Func<decimal, decimal, decimal>>)((x, y) => x * y), Args(1.5m, 2.5m));
    }

    [Test]
    public void Checked_arithmetic()
    {
        var x = Expression.Parameter(typeof(int), "x");
        var y = Expression.Parameter(typeof(int), "y");

        Executes(Expression.Lambda<Func<int, int, int>>(Expression.AddChecked(x, y), x, y), Args(1, 2), Args(int.MaxValue, 1));
        Executes(Expression.Lambda<Func<int, int, int>>(Expression.SubtractChecked(x, y), x, y), Args(int.MinValue, 1));
        Executes(Expression.Lambda<Func<int, int, int>>(Expression.MultiplyChecked(x, y), x, y), Args(1 << 20, 1 << 20));
        Executes(Expression.Lambda<Func<int, int>>(Expression.NegateChecked(x), x), Args(5), Args(int.MinValue));
    }

    [Test]
    public void Bitwise_and_shift_operators()
    {
        Executes((Expression<Func<int, int, int>>)((x, y) => x & y), Args(0b1100, 0b1010));
        Executes((Expression<Func<int, int, int>>)((x, y) => x | y), Args(0b1100, 0b1010));
        Executes((Expression<Func<int, int, int>>)((x, y) => x ^ y), Args(0b1100, 0b1010));
        Executes((Expression<Func<int, int>>)(x => ~x), Args(0b1100));
        Executes((Expression<Func<int, int, int>>)((x, y) => x << y), Args(1, 5));
        Executes((Expression<Func<int, int, int>>)((x, y) => x >> y), Args(1024, 3));
        Executes((Expression<Func<bool, bool, bool>>)((x, y) => x ^ y), Args(true, false), Args(true, true));
    }

    [Test]
    public void Logical_operators()
    {
        Executes((Expression<Func<bool, bool, bool>>)((x, y) => x && y), Args(true, true), Args(true, false));
        Executes((Expression<Func<bool, bool, bool>>)((x, y) => x || y), Args(false, false), Args(false, true));
        Executes((Expression<Func<bool, bool>>)(x => !x), Args(true), Args(false));
        Executes((Expression<Func<int, bool>>)(x => x > 2 && x < 10 || x == 0), Args(5), Args(0), Args(50));
    }

    [Test]
    public void Comparison_operators()
    {
        Executes((Expression<Func<int, int, bool>>)((x, y) => x == y), Args(3, 3), Args(3, 4));
        Executes((Expression<Func<int, int, bool>>)((x, y) => x != y), Args(3, 3), Args(3, 4));
        Executes((Expression<Func<int, int, bool>>)((x, y) => x > y), Args(5, 3), Args(3, 5));
        Executes((Expression<Func<int, int, bool>>)((x, y) => x >= y), Args(3, 3));
        Executes((Expression<Func<int, int, bool>>)((x, y) => x < y), Args(3, 5));
        Executes((Expression<Func<int, int, bool>>)((x, y) => x <= y), Args(5, 3));
        Executes((Expression<Func<string, string, bool>>)((x, y) => x == y), Args("a", "a"), Args("a", "b"));
        Executes((Expression<Func<DateTime, DateTime, bool>>)((x, y) => x > y), Args(new DateTime(2026, 1, 2), new DateTime(2026, 1, 1)));
    }

    [Test]
    public void Lifted_nullable_operators()
    {
        Executes((Expression<Func<int?, int?, int?>>)((x, y) => x + y), Args(3, 4), Args(null, 4), Args(3, null));
        Executes((Expression<Func<int?, bool>>)(x => x > 5), Args(10), Args(2), Args((object?)null));
        Executes((Expression<Func<int?, int?, bool>>)((x, y) => x == y), Args(null, null), Args(1, null), Args(1, 1));

        // LiftToNull: comparison producing bool? instead of bool.
        var x1 = Expression.Parameter(typeof(int?), "x");
        var y1 = Expression.Parameter(typeof(int?), "y");
        var lifted = Expression.Lambda<Func<int?, int?, bool?>>(
            Expression.MakeBinary(ExpressionType.GreaterThan, x1, y1, liftToNull: true, method: null), x1, y1);
        Executes(lifted, Args(5, 3), Args(null, 3), Args(3, null));
    }

    [Test]
    public void Coalesce_operator()
    {
        Executes((Expression<Func<int?, int>>)(x => x ?? -1), Args(7), Args((object?)null));
        Executes((Expression<Func<string?, string>>)(x => x ?? "fallback"), Args("value"), Args((object?)null));

        // Coalesce with a conversion lambda.
        var parameter = Expression.Parameter(typeof(string), "s");
        var inner = Expression.Parameter(typeof(string), "v");
        var conversion = Expression.Lambda(Expression.Property(inner, nameof(string.Length)), inner);

        var coalesce = Expression.Lambda<Func<string?, int>>(
            Expression.Coalesce(parameter, Expression.Constant(-1), conversion), parameter);
        Executes(coalesce, Args("abcd"), Args((object?)null));
    }

    [Test]
    public void Conditional_operator()
    {
        Executes((Expression<Func<int, string>>)(x => x > 0 ? "pos" : "neg"), Args(5), Args(-5));
        Executes((Expression<Func<int, int>>)(x => x > 100 ? x * 2 : x > 10 ? x + 1 : 0), Args(200), Args(50), Args(5));
    }

    [Test]
    public void Power_operator()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var y = Expression.Parameter(typeof(double), "y");
        Executes(Expression.Lambda<Func<double, double, double>>(Expression.Power(x, y), x, y), Args(2.0, 10.0));
    }

    [Test]
    public void User_defined_operators()
    {
        Executes((Expression<Func<Money, Money, Money>>)((a, b) => a + b), Args(new Money(10.5m), new Money(4.5m)));
        Executes((Expression<Func<Money, Money, bool>>)((a, b) => a > b), Args(new Money(10m), new Money(4m)), Args(new Money(1m), new Money(4m)));
        Executes((Expression<Func<Money, Money>>)(a => -a), Args(new Money(9.9m)));
    }

    [Test]
    public void Unary_operators()
    {
        Executes((Expression<Func<int, int>>)(x => -x), Args(42));
        Executes((Expression<Func<double, double>>)(x => -x), Args(-3.5));

        var x = Expression.Parameter(typeof(int), "x");
        Executes(Expression.Lambda<Func<int, int>>(Expression.UnaryPlus(x), x), Args(7));
        Executes(Expression.Lambda<Func<int, int>>(Expression.Increment(x), x), Args(7));
        Executes(Expression.Lambda<Func<int, int>>(Expression.Decrement(x), x), Args(7));
        Executes(Expression.Lambda<Func<int, int>>(Expression.OnesComplement(x), x), Args(7));

        var b = Expression.Parameter(typeof(bool), "b");
        Executes(Expression.Lambda<Func<bool, bool>>(Expression.IsTrue(b), b), Args(true), Args(false));
        Executes(Expression.Lambda<Func<bool, bool>>(Expression.IsFalse(b), b), Args(true), Args(false));
    }

    [Test]
    public void Conversion_operators()
    {
        Executes((Expression<Func<int, long>>)(x => x), Args(42)); // implicit widening → Convert
        Executes((Expression<Func<double, int>>)(x => (int)x), Args(3.99));
        Executes((Expression<Func<int, byte>>)(x => (byte)x), Args(200));
        Executes((Expression<Func<OrderStatus, int>>)(s => (int)s), Args(OrderStatus.Shipped));
        Executes((Expression<Func<int, OrderStatus>>)(x => (OrderStatus)x), Args(2));
        Executes((Expression<Func<int, object>>)(x => x), Args(5)); // boxing
        Executes((Expression<Func<object, string>>)(o => (string)o), Args("cast me"));
        Executes((Expression<Func<int?, int>>)(x => (int)x!), Args(9));
        Executes((Expression<Func<int, int?>>)(x => x), Args(9));

        var x1 = Expression.Parameter(typeof(int), "x");
        Executes(Expression.Lambda<Func<int, byte>>(Expression.ConvertChecked(x1, typeof(byte)), x1), Args(200), Args(300));
    }

    [Test]
    public void Type_test_operators()
    {
        Executes((Expression<Func<object, bool>>)(o => o is string), Args("text"), Args(42), Args((object?)null));
        Executes((Expression<Func<object, string?>>)(o => o as string), Args("text"), Args(42));
        Executes((Expression<Func<object, bool>>)(o => o is Order), Args(new Order()), Args("nope"));

        // TypeEqual is only reachable through the factory API.
        var p = Expression.Parameter(typeof(object), "o");
        Executes(
            Expression.Lambda<Func<object, bool>>(Expression.TypeEqual(p, typeof(string)), p),
            Args("text"),
            Args(42));
    }

    [Test]
    public void Unbox_operator()
    {
        var p = Expression.Parameter(typeof(object), "o");
        Executes(Expression.Lambda<Func<object, int>>(Expression.Unbox(p, typeof(int)), p), Args(123));
    }

    [Test]
    public void Array_operators()
    {
        Executes((Expression<Func<int[], int>>)(xs => xs.Length), Args(new object[] { new[] { 1, 2, 3 } }));
        Executes((Expression<Func<int[], int, int>>)((xs, i) => xs[i]), Args(new[] { 10, 20, 30 }, 1));
        Executes((Expression<Func<string[], string>>)(xs => xs[0]), Args(new object[] { new[] { "first", "second" } }));
    }

    [Test]
    public void String_concatenation_uses_method_binary()
    {
        Executes((Expression<Func<string, string, string>>)((a, b) => a + b), Args("foo", "bar"));
        Executes((Expression<Func<string, int, string>>)((a, b) => a + b), Args("n=", 42));
    }

    [Test]
    public void Enum_flags_and_comparisons()
    {
        Executes((Expression<Func<Order, bool>>)(o => o.Status == OrderStatus.Paid), Args(TestData.Orders[0]), Args(TestData.Orders[1]));
        Executes((Expression<Func<Order, bool>>)(o => o.Tags.HasFlag(OrderTags.Gift)), Args(TestData.Orders[0]), Args(TestData.Orders[1]));
        Executes((Expression<Func<Order, bool>>)(o => (o.Tags & OrderTags.Express) != 0), Args(TestData.Orders[0]), Args(TestData.Orders[3]));
    }
}
