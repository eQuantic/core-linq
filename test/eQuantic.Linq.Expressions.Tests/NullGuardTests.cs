using System.Linq.Expressions;
using eQuantic.Linq.Expressions.Tests.TestModel;
using eQuantic.Linq.Web;

namespace eQuantic.Linq.Expressions.Tests;

/// <summary>Null propagation for pure LINQ-to-objects, auto-detected at Apply time.</summary>
[TestFixture]
public class NullGuardTests
{
    private static List<int> Ids(IQueryable<Order> q) => q.Select(o => o.Id).ToList();

    [Test]
    public void Auto_mode_guards_nested_paths_over_enumerable_query()
    {
        // Diego (order 5) has a null Address: unguarded this would throw NRE in memory.
        Assert.That(Ids(TestData.OrdersQuery.WhereQueryString("customer.address.city:eq(Lisboa)")), Is.EqualTo(new[] { 1, 4 }));
        Assert.That(Ids(TestData.OrdersQuery.WhereQueryString("notes.toLower():ct(gift)")), Is.EqualTo(new[] { 4 }));
        Assert.That(Ids(TestData.OrdersQuery.WhereQueryString("customer.address.city.length:gte(6)")), Is.EqualTo(new[] { 1, 3, 4, 6 }));
    }

    [Test]
    public void Never_mode_preserves_raw_csharp_semantics()
    {
        var options = new QueryStringOptions { NullGuards = NullGuardMode.Never };
        var query = TestData.OrdersQuery.WhereQueryString("customer.address.city:eq(Lisboa)", options);

        Assert.Throws<NullReferenceException>(() => query.ToList());
    }

    [Test]
    public void Rewriter_is_usable_directly_on_csharp_predicates()
    {
        Expression<Func<Order, bool>> raw = o => o.Customer.Address.City == "Lisboa";
        var guarded = eQuantic.Linq.Expressions.NullGuards.Apply(raw).Compile();

        Assert.That(guarded(TestData.Orders[4]), Is.False, "null navigation propagates to false");
        Assert.That(guarded(TestData.Orders[0]), Is.True);
    }
}
