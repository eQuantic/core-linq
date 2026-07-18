using eQuantic.Linq.Expressions.Tests.TestModel;
using eQuantic.Linq.Web;

namespace eQuantic.Linq.Expressions.Tests;

[TestFixture]
public class QueryCollectionTests
{
    [Test]
    public void QueryFilterCollection_TryParse_builds_typed_models()
    {
        var parsed = QueryFilterCollection<Order>.TryParse("total:gt(100)", null, out var collection);

        Assert.Multiple(() =>
        {
            Assert.That(parsed, Is.True);
            Assert.That(collection, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void QueryFilterCollection_TryParse_rejects_invalid_syntax()
    {
        var parsed = QueryFilterCollection<Order>.TryParse("total:nope(100)", null, out var collection);

        Assert.Multiple(() =>
        {
            Assert.That(parsed, Is.False);
            Assert.That(collection, Is.Null);
        });
    }

    [Test]
    public void QueryFilterCollection_ToPredicate_combines_items_with_and()
    {
        QueryFilterCollection<Order>.TryParse("total:gt(100)", null, out var collection);
        QueryFilterCollection<Order>.TryParse("customer.name:ct(li)", null, out var second);
        collection!.AddRange(second!);

        var predicate = collection.ToPredicate()!.Compile();

        Assert.Multiple(() =>
        {
            Assert.That(predicate(new Order { Total = 150, Customer = new Customer { Name = "Alice" } }), Is.True);
            Assert.That(predicate(new Order { Total = 150, Customer = new Customer { Name = "Bob" } }), Is.False);
            Assert.That(predicate(new Order { Total = 50, Customer = new Customer { Name = "Alice" } }), Is.False);
        });
    }

    [Test]
    public void QueryFilterCollection_serializes_as_a_list_of_models()
    {
        QueryFilterCollection<Order>.TryParse("total:gt(100)", null, out var collection);

        var json = System.Text.Json.JsonSerializer.Serialize(collection);
        var revived = System.Text.Json.JsonSerializer.Deserialize<QueryFilterCollection<Order>>(json);

        Assert.Multiple(() =>
        {
            Assert.That(revived, Has.Count.EqualTo(1));
            Assert.That(revived!.ToPredicate()!.Compile()(new Order { Total = 150 }), Is.True);
        });
    }

    [Test]
    public void QuerySortCollection_TryParse_builds_typed_sorts()
    {
        var parsed = QuerySortCollection<Order>.TryParse("total:desc,customer.name", null, out var collection);

        Assert.Multiple(() =>
        {
            Assert.That(parsed, Is.True);
            Assert.That(collection, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void QuerySortCollection_TryParse_rejects_invalid_syntax()
    {
        var parsed = QuerySortCollection<Order>.TryParse("total:sideways", null, out var collection);

        Assert.Multiple(() =>
        {
            Assert.That(parsed, Is.False);
            Assert.That(collection, Is.Null);
        });
    }
}
