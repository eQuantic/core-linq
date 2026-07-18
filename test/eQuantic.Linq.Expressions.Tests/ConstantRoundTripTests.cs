using System.Linq.Expressions;
using eQuantic.Linq.Expressions.Tests.Support;
using eQuantic.Linq.Expressions.Tests.TestModel;

namespace eQuantic.Linq.Expressions.Tests;

[TestFixture]
public class ConstantRoundTripTests
{
    private static void Const<T>(T value)
    {
        var lambda = Expression.Lambda<Func<T>>(Expression.Constant(value, typeof(T)));
        Verify.Executes(lambda);
    }

    [Test]
    public void Integral_types()
    {
        Const<byte>(255);
        Const<sbyte>(-128);
        Const<short>(-32768);
        Const<ushort>(65535);
        Const(int.MinValue);
        Const(int.MaxValue);
        Const<uint>(4294967295);
        Const(long.MinValue);
        Const(long.MaxValue);
        Const(ulong.MaxValue);
    }

    [Test]
    public void Floating_point_types()
    {
        Const(3.14159);
        Const(-1.5f);
        Const(double.Epsilon);
        Const(double.MaxValue);
        Const(double.NaN);
        Const(double.PositiveInfinity);
        Const(double.NegativeInfinity);
        Const(float.NaN);
    }

    [Test]
    public void Decimal_precision()
    {
        Const(decimal.MaxValue);
        Const(decimal.MinValue);
        Const(0.0000000000000000000000000001m);
        Const(1234567.891234m);
    }

    [Test]
    public void Text_types()
    {
        Const("hello");
        Const(string.Empty);
        Const("unicode: ção 💡 \"quoted\" \\slash\\ \n line");
        Const('x');
        Const('ç');
    }

    [Test]
    public void Boolean_values()
    {
        Const(true);
        Const(false);
    }

    [Test]
    public void Temporal_and_id_types()
    {
        Const(new Guid("9d2a1c3e-4b5f-6a7d-8e9f-0a1b2c3d4e5f"));
        Const(new DateTime(2026, 7, 18, 10, 30, 45, DateTimeKind.Utc));
        Const(new DateTime(2026, 7, 18, 10, 30, 45, DateTimeKind.Unspecified));
        Const(new DateTimeOffset(2026, 7, 18, 10, 30, 45, TimeSpan.FromHours(-3)));
        Const(new TimeSpan(1, 2, 3, 4, 5));
        Const(TimeSpan.Zero);
        Const(new DateOnly(2026, 7, 18));
        Const(new TimeOnly(23, 59, 58));
        Const(new Uri("https://equantic.tech/path?q=1"));
    }

    [Test]
    public void Enum_values()
    {
        Const(OrderStatus.Shipped);
        Const(OrderTags.Gift | OrderTags.International);
        Const((OrderStatus)42);
        Const(DayOfWeek.Friday);
    }

    [Test]
    public void Nullable_values()
    {
        Const<int?>(42);
        Const<int?>(null);
        Const<decimal?>(10.5m);
        Const<Guid?>(Guid.Empty);
        Const<OrderStatus?>(OrderStatus.Paid);
        Const<OrderStatus?>(null);
        Const<DateTime?>(null);
    }

    [Test]
    public void Null_references()
    {
        Const<string?>(null);
        Const<Order?>(null);
        Const<int[]?>(null);
        Const<object?>(null);
    }

    [Test]
    public void Collections()
    {
        Const(new[] { 1, 2, 3 });
        Const(new[] { "a", "b" });
        Const(Array.Empty<int>());
        Const(new byte[] { 1, 2, 255 });
        Const(new List<int> { 5, 6, 7 });
        Const(new List<string?> { "x", null });
        Const(new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 });
    }

    [Test]
    public void Complex_object_graphs()
    {
        Const(new OrderItem { Id = 9, Product = "Lamp", Category = "Home", Price = 49.9m, Quantity = 3 });
        Const(TestData.Orders[0]);
        Const(new List<OrderItem>
        {
            new() { Id = 1, Product = "A", Price = 1m, Quantity = 1 },
            new() { Id = 2, Product = "B", Price = 2m, Quantity = 2 },
        });
        Const(new Money(150.75m));
    }

    [Test]
    public void Boxed_constant_keeps_runtime_type()
    {
        var lambda = Expression.Lambda<Func<object>>(Expression.Constant(5, typeof(object)));
        var rebuilt = Verify.RoundTrip(lambda);

        var value = rebuilt.Compile().DynamicInvoke();
        Assert.That(value, Is.TypeOf<int>());
        Assert.That(value, Is.EqualTo(5));
    }

    [Test]
    public void Anonymous_type_constant_rebuilds_with_emitted_type()
    {
        var constant = Expression.Constant(new { Id = 7, Name = "seven" });
        var lambda = Expression.Lambda(constant);

        var rebuilt = Verify.RoundTrip(lambda, checkStructure: false);
        var value = rebuilt.Compile().DynamicInvoke();

        Assert.That(value, Is.Not.Null);
        Assert.That(value!.ToString(), Is.EqualTo("{ Id = 7, Name = seven }"));
    }

    [Test]
    public void Expression_valued_constant_is_supported()
    {
        Expression<Func<int, int>> inner = x => x * 2;
        var constant = Expression.Constant(inner, typeof(Expression<Func<int, int>>));
        var lambda = Expression.Lambda<Func<Expression<Func<int, int>>>>(constant);

        var rebuilt = Verify.RoundTrip(lambda);
        var recovered = rebuilt.Compile()();

        Assert.That(recovered.Compile()(21), Is.EqualTo(42));
    }

    [Test]
    public void Delegate_constant_is_rejected_with_clear_error()
    {
        Func<int, int> del = x => x;
        var lambda = Expression.Lambda(Expression.Constant(del));

        var serializer = Verify.Serializer(o => o.EnablePartialEvaluation = false);
        var exception = Assert.Throws<ExpressionSerializationException>(() => serializer.ToJson(lambda));
        Assert.That(exception!.Message, Does.Contain("Delegate"));
    }
}
