using System.Reflection;
using eQuantic.Linq.Expressions.Tests.TestModel;
using eQuantic.Linq.Web.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
    public async Task BindAsync_uses_the_from_query_attribute_key()
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?filterBy=total:gt(100)&filterBy=customer.isVip:true&orderBy=total:desc");

        var filters = await QueryFilterCollection<Order>.BindAsync(context, ParameterOf(nameof(AttributeProbe)));
        var sorts = await QuerySortCollection<Order>.BindAsync(context, SortParameterOf(nameof(AttributeSortProbe)));

        Assert.Multiple(() =>
        {
            Assert.That(filters, Has.Count.EqualTo(2), "both filterBy values bound via [FromQuery(Name)]");
            Assert.That(sorts, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task BindAsync_falls_back_to_the_parameter_name()
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?filter=total:gt(100)");

        var filters = await QueryFilterCollection<Order>.BindAsync(context, ParameterOf(nameof(NameProbe)));

        Assert.That(filters, Has.Count.EqualTo(1));
    }

    private static void AttributeProbe([FromQuery(Name = "filterBy")] QueryFilterCollection<Order>? filter) { }
    private static void AttributeSortProbe([FromQuery(Name = "orderBy")] QuerySortCollection<Order>? sort) { }
    private static void NameProbe(QueryFilterCollection<Order>? filter) { }

    private static ParameterInfo ParameterOf(string method) =>
        typeof(QueryCollectionTests).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static)!.GetParameters()[0];

    private static ParameterInfo SortParameterOf(string method) => ParameterOf(method);
}
