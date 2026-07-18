using System.Linq.Expressions;
using eQuantic.Linq.Expressions.Tests.Support;
using eQuantic.Linq.Expressions.Tests.TestModel;
using static eQuantic.Linq.Expressions.Tests.Support.Verify;

namespace eQuantic.Linq.Expressions.Tests;

[TestFixture]
public class ObjectCreationTests
{
    [Test]
    public void New_with_constructor_arguments()
    {
        Executes((Expression<Func<string, decimal, OrderItem>>)((p, v) => new OrderItem(p, v, 2)), Args("Desk", 300m));
        Executes((Expression<Func<int, TimeSpan>>)(h => new TimeSpan(h, 30, 0)), Args(2));
        Executes((Expression<Func<string, Uri>>)(u => new Uri(u)), Args("https://equantic.tech/"));
    }

    [Test]
    public void New_parameterless()
    {
        Executes((Expression<Func<Customer>>)(() => new Customer()));
        Executes((Expression<Func<List<int>>>)(() => new List<int>()));
        Executes((Expression<Func<TimeSpan>>)(() => new TimeSpan()));
        Executes((Expression<Func<Money>>)(() => new Money()));
    }

    [Test]
    public void Member_init_flat()
    {
        Executes(
            (Expression<Func<int, decimal, Order>>)((id, total) => new Order
            {
                Id = id,
                Total = total,
                Status = OrderStatus.Paid,
                Notes = "created",
            }),
            Args(10, 99.9m));
    }

    [Test]
    public void Member_init_nested_member_binding()
    {
        Executes(
            (Expression<Func<string, Order>>)(name => new Order
            {
                Id = 1,
                Customer = { Name = name, Age = 30 },
            }),
            Args("Nested"));
    }

    [Test]
    public void Member_init_list_binding()
    {
        Executes(
            (Expression<Func<decimal, Order>>)(price => new Order
            {
                Id = 2,
                Items =
                {
                    new OrderItem { Product = "First", Price = price, Quantity = 1 },
                    new OrderItem { Product = "Second", Price = price * 2, Quantity = 2 },
                },
            }),
            Args(10m));
    }

    [Test]
    public void Member_init_deeply_combined()
    {
        Executes(
            (Expression<Func<Order, Order>>)(source => new Order
            {
                Id = source.Id + 1000,
                Status = source.Status,
                Customer =
                {
                    Name = source.Customer.Name.ToLower(),
                    Address = new Address { City = "Copied " + source.Customer.Name },
                },
                Items = { new OrderItem { Product = "Bonus", Price = 0m, Quantity = 1 } },
            }),
            Args(TestData.Orders[0]));
    }

    [Test]
    public void List_init()
    {
        Executes((Expression<Func<int, List<int>>>)(x => new List<int> { 1, x, x * 2 }), Args(5));
        Executes(
            (Expression<Func<string, Dictionary<string, int>>>)(key => new Dictionary<string, int>
            {
                { "fixed", 1 },
                { key, 2 },
            }),
            Args("dynamic"));
    }

    [Test]
    public void New_array_init()
    {
        Executes((Expression<Func<int, int[]>>)(x => new[] { x, x + 1, x + 2 }), Args(10));
        Executes((Expression<Func<string, string[]>>)(s => new[] { s, s.ToUpper() }), Args("mix"));
        Executes((Expression<Func<int, int[][]>>)(x => new[] { new[] { x }, new[] { x, x } }), Args(3));
        Executes((Expression<Func<object[]>>)(() => new object[] { 1, "two", 3.0 }));
    }

    [Test]
    public void New_array_bounds()
    {
        Executes((Expression<Func<int, int[]>>)(n => new int[n]), Args(4));

        // Multi-dimensional arrays cannot be JSON-compared; assert shape manually.
        var lambda = (Expression<Func<int, int, string[,]>>)((a, b) => new string[a, b]);
        var rebuilt = (Expression<Func<int, int, string[,]>>)Verify.RoundTrip(lambda);
        var matrix = rebuilt.Compile()(2, 3);
        Assert.That(matrix.GetLength(0), Is.EqualTo(2));
        Assert.That(matrix.GetLength(1), Is.EqualTo(3));
    }

    [Test]
    public void Anonymous_type_projection()
    {
        ExecutesShapeless(
            (Expression<Func<Order, object>>)(o => new { o.Id, Name = o.Customer.Name.ToUpper(), Items = o.Items.Count }),
            Args(TestData.Orders[0]),
            Args(TestData.Orders[2]));
    }

    [Test]
    public void Anonymous_type_nested()
    {
        ExecutesShapeless(
            (Expression<Func<Order, object>>)(o => new
            {
                o.Id,
                Customer = new { o.Customer.Name, City = o.Customer.Address != null ? o.Customer.Address.City : "?" },
                Stats = new { Count = o.Items.Count, Max = o.Items.Count > 0 ? o.Items.Max(i => i.Price) : 0m },
            }),
            Args(TestData.Orders[0]),
            Args(TestData.Orders[4]));
    }

    [Test]
    public void Anonymous_type_equality_semantics_survive()
    {
        // Two lambdas producing the same anonymous shape must yield instances that are
        // structurally equal — that is what GroupBy/Distinct rely on.
        var lambda = (Expression<Func<Order, object>>)(o => new { o.Status, o.Total });
        var rebuilt = Verify.RoundTrip(lambda, checkStructure: false);
        var projector = (Func<Order, object>)rebuilt.Compile();

        var first = projector(TestData.Orders[0]);  // Paid, 250.50
        var duplicate = projector(TestData.Orders[5]); // Paid, 250.50 (same values on purpose)
        var different = projector(TestData.Orders[1]);

        Assert.That(first, Is.EqualTo(duplicate));
        Assert.That(first.GetHashCode(), Is.EqualTo(duplicate.GetHashCode()));
        Assert.That(first, Is.Not.EqualTo(different));
        Assert.That(first.ToString(), Does.Contain("Status = Paid"));
    }
}
