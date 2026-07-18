using System.Linq.Expressions;
using eQuantic.Linq.Expressions.Tests.Support;
using eQuantic.Linq.Expressions.Tests.TestModel;
using eQuantic.Linq.Web;

namespace eQuantic.Linq.Expressions.Tests;

/// <summary>
/// The query-string syntax layer (eQuantic.Linq.Web): every filter is validated against the
/// equivalent hand-written LINQ expression executed over the same data.
/// </summary>
[TestFixture]
public class QueryStringSyntaxTests
{
    private static List<int> Ids(IQueryable<Order> query) => query.Select(o => o.Id).ToList();

    private static void AssertFilter(string filter, Expression<Func<Order, bool>> equivalent)
    {
        var actual = Ids(TestData.OrdersQuery.WhereQueryString(filter));
        var expected = Ids(TestData.OrdersQuery.Where(equivalent));

        Assert.That(actual, Is.EqualTo(expected), $"filter: {filter}");
    }

    // ---------------------------------------------------------------- operators

    [Test]
    public void Equality_operators()
    {
        AssertFilter("customer.name:eq(Alice)", o => o.Customer.Name == "Alice");
        AssertFilter("total:neq(250.50)", o => o.Total != 250.50m);
        AssertFilter("id:3", o => o.Id == 3); // shorthand
    }

    [Test]
    public void Comparison_operators()
    {
        AssertFilter("total:gt(300)", o => o.Total > 300m);
        AssertFilter("total:gte(250.50)", o => o.Total >= 250.50m);
        AssertFilter("total:lt(100)", o => o.Total < 100m);
        AssertFilter("total:lte(75)", o => o.Total <= 75m);
        AssertFilter("priority:gte(2)", o => o.Priority >= 2);
    }

    [Test]
    public void String_operators()
    {
        AssertFilter("customer.name:ct(ar)", o => o.Customer.Name.Contains("ar"));
        AssertFilter("customer.name:sw(A)", o => o.Customer.Name.StartsWith("A"));
        AssertFilter("customer.name:ew(a)", o => o.Customer.Name.EndsWith("a"));
        AssertFilter("customer.name:nct(l)", o => !o.Customer.Name.Contains("l"));
    }

    [Test]
    public void Enum_bool_guid_and_dates()
    {
        AssertFilter("status:eq(Paid)", o => o.Status == OrderStatus.Paid);
        AssertFilter("status:neq(Cancelled)", o => o.Status != OrderStatus.Cancelled);
        AssertFilter("customer.isVip:eq(true)", o => o.Customer.IsVip);
        AssertFilter("customer.isVip:true", o => o.Customer.IsVip);
        AssertFilter("reference:eq(aaaaaaaa-0000-0000-0000-000000000003)", o => o.Reference == new Guid("aaaaaaaa-0000-0000-0000-000000000003"));
        AssertFilter("createdAt:gte(2026-02-01)", o => o.CreatedAt >= new DateTime(2026, 2, 1));
        AssertFilter("createdAt:gt(2026-01-10T08:00:00)", o => o.CreatedAt > new DateTime(2026, 1, 10, 8, 0, 0));
    }

    [Test]
    public void Null_handling_over_nullables()
    {
        AssertFilter("discount:eq(null)", o => o.Discount == null);
        AssertFilter("discount:neq(null)", o => o.Discount != null);
        AssertFilter("discount:gte(20)", o => o.Discount >= 20m);
        AssertFilter("deliveredAt:neq(null)", o => o.DeliveredAt != null);
        AssertFilter("notes:eq(null)", o => o.Notes == null);
    }

    // ---------------------------------------------------------------- composition

    [Test]
    public void Logical_composition()
    {
        AssertFilter("or(status:eq(Paid),total:gt(1000))", o => o.Status == OrderStatus.Paid || o.Total > 1000m);
        AssertFilter("and(total:gt(100),status:neq(Cancelled))", o => o.Total > 100m && o.Status != OrderStatus.Cancelled);
        AssertFilter("total:gt(100),status:neq(Cancelled)", o => o.Total > 100m && o.Status != OrderStatus.Cancelled);
        AssertFilter("not(status:eq(Paid))", o => o.Status != OrderStatus.Paid);
        AssertFilter(
            "or(and(status:eq(Paid),total:gt(300)),customer.age:lt(20))",
            o => (o.Status == OrderStatus.Paid && o.Total > 300m) || o.Customer.Age < 20);
    }

    [Test]
    public void Collection_any_and_all()
    {
        AssertFilter("items:any(price:gt(500))", o => o.Items.Any(i => i.Price > 500m));
        AssertFilter("items:any(category:eq(Tech),price:lt(100))", o => o.Items.Any(i => i.Category == "Tech" && i.Price < 100m));
        AssertFilter("items:all(quantity:gte(1))", o => o.Items.All(i => i.Quantity >= 1));
        AssertFilter("items:any()", o => o.Items.Any());
        AssertFilter(
            "items:any(or(category:eq(Home),price:gt(800)))",
            o => o.Items.Any(i => i.Category == "Home" || i.Price > 800m));
    }

    [Test]
    public void Collection_aggregates()
    {
        AssertFilter("items.count():gt(1)", o => o.Items.Count() > 1);
        AssertFilter("items.count(price:gt(100)):gte(1)", o => o.Items.Count(i => i.Price > 100m) >= 1);
        AssertFilter("items.sum(price):gt(200)", o => o.Items.Sum(i => i.Price) > 200m);
        AssertFilter(
            "and(items:any(),items.max(price):gte(899))",
            o => o.Items.Any() && o.Items.Max(i => i.Price) >= 899m);
        AssertFilter(
            "and(items:any(),items.min(price):lt(70))",
            o => o.Items.Any() && o.Items.Min(i => i.Price) < 70m);
        AssertFilter(
            "and(items:any(),items.average(price):gt(90))",
            o => o.Items.Any() && o.Items.Average(i => i.Price) > 90m);
        AssertFilter(
            "and(items:any(),items.avg(price):gt(90))",
            o => o.Items.Any() && o.Items.Average(i => i.Price) > 90m);
        AssertFilter("items.sum(quantity):gte(3)", o => o.Items.Sum(i => i.Quantity) >= 3);
    }

    [Test]
    public void Method_segments_in_paths()
    {
        AssertFilter(
            "and(notes:neq(null),notes.toLower():ct(gift))",
            o => o.Notes != null && o.Notes.ToLower().Contains("gift"));
        AssertFilter("customer.name.length:gte(5)", o => o.Customer.Name.Length >= 5);
        AssertFilter("customer.name.substring(0,3):eq(Ali)", o => o.Customer.Name.Substring(0, 3) == "Ali");
        AssertFilter("customer.name.toUpper():sw(CAR)", o => o.Customer.Name.ToUpper().StartsWith("CAR"));
    }

    [Test]
    public void Membership_in_and_nin()
    {
        AssertFilter("status:in(Paid|Shipped)", o => o.Status == OrderStatus.Paid || o.Status == OrderStatus.Shipped);
        AssertFilter("id:nin(1|2|3)", o => o.Id != 1 && o.Id != 2 && o.Id != 3);
        AssertFilter("status:in( Paid | Shipped )", o => o.Status == OrderStatus.Paid || o.Status == OrderStatus.Shipped);
        AssertFilter("customer.name:in('Alice'|'Bruno')", o => o.Customer.Name == "Alice" || o.Customer.Name == "Bruno");
    }

    [Test]
    public void Quoted_values()
    {
        // LINQ-to-objects semantics are preserved: guard nullable strings before calling methods on them.
        AssertFilter(
            "and(notes:neq(null),notes:ct('Leave at'))",
            o => o.Notes != null && o.Notes.Contains("Leave at"));
        AssertFilter("notes:eq('it''s')", o => o.Notes == "it's");
        AssertFilter("customer.name:eq('Alice')", o => o.Customer.Name == "Alice");
    }

    // ---------------------------------------------------------------- sorting / paging / projection

    [Test]
    public void Sorting()
    {
        var actual = Ids(TestData.OrdersQuery.OrderByQueryString("total:desc,id"));
        var expected = Ids(TestData.OrdersQuery.OrderByDescending(o => o.Total).ThenBy(o => o.Id));
        Assert.That(actual, Is.EqualTo(expected));
        Assert.That(actual, Is.EqualTo(new[] { 3, 5, 1, 6, 4, 2 }));

        var byName = Ids(TestData.OrdersQuery.OrderByQueryString("customer.name.toLower():desc,id:asc"));
        var byNameExpected = Ids(TestData.OrdersQuery.OrderByDescending(o => o.Customer.Name.ToLower()).ThenBy(o => o.Id));
        Assert.That(byName, Is.EqualTo(byNameExpected));

        var sorts = QuerySort<Order>.Parse("total:desc,customer.name");
        Assert.That(sorts, Has.Count.EqualTo(2));
        Assert.That(sorts[0].Path, Is.EqualTo("total"));
        Assert.That(sorts[0].Direction, Is.EqualTo(SortDirection.Descending));
        Assert.That(sorts[1].Direction, Is.EqualTo(SortDirection.Ascending));
    }

    [Test]
    public void Full_query_string_with_paging()
    {
        var query = EntityQuery.Parse<Order>("?filter=status:neq(Cancelled)&orderBy=total:desc&skip=1&take=2");

        Assert.That(query.Skip, Is.EqualTo(1));
        Assert.That(query.Take, Is.EqualTo(2));

        var actual = Ids(query.Apply(TestData.OrdersQuery));
        var expected = Ids(TestData.OrdersQuery
            .Where(o => o.Status != OrderStatus.Cancelled)
            .OrderByDescending(o => o.Total)
            .Skip(1)
            .Take(2));

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Url_encoded_values_and_repeated_filter_keys()
    {
        var actual = Ids(TestData.OrdersQuery.ApplyQueryString("?filter=total%3Agt(100)&filter=status%3Aeq(Paid)&orderBy=id"));
        var expected = Ids(TestData.OrdersQuery.Where(o => o.Total > 100m && o.Status == OrderStatus.Paid).OrderBy(o => o.Id));

        Assert.That(actual, Is.EqualTo(expected));
        Assert.That(actual, Is.EqualTo(new[] { 1, 6 }));
    }

    [Test]
    public void Selection_projects_to_emitted_anonymous_types()
    {
        var query = EntityQuery.Parse<Order>("?filter=id:lte(2)&orderBy=id&select=id,customer.name,items.count()");
        var results = query.ApplyWithSelection(TestData.OrdersQuery).Cast<object>().ToList();

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results[0].ToString(), Is.EqualTo("{ Id = 1, CustomerName = Alice, ItemsCount = 2 }"));
        Assert.That(results[1].ToString(), Is.EqualTo("{ Id = 2, CustomerName = Bruno, ItemsCount = 1 }"));
    }

    [Test]
    public void Selection_supports_aliases()
    {
        var results = TestData.OrdersQuery
            .Where(o => o.Id == 1)
            .SelectQueryString("name=customer.name,revenue=items.sum(price)")
            .Cast<object>()
            .ToList();

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].ToString(), Is.EqualTo("{ name = Alice, revenue = 185.25 }"));
    }

    // ---------------------------------------------------------------- transport bridge

    [Test]
    public void Filter_model_bridges_to_json_and_back()
    {
        var serializer = Verify.Serializer();

        // querystring → model → JSON → model → predicate: the full transport chain.
        var model = QueryFilter.ParseModel<Order>("items:any(price:gt(500))");
        var json = serializer.ToJson(model);

        Assert.That(json, Does.Contain("\"Any\""));
        Assert.That(json, Does.Not.Contain("declaringType"), "querystring models stay lean");

        var predicate = serializer.ModelFromJson<Order>(json).ToPredicate(serializer);
        Assert.That(Ids(TestData.OrdersQuery.Where(predicate)), Is.EqualTo(new[] { 3 }));
    }

    [Test]
    public void Entity_query_exposes_the_filter_model()
    {
        var query = EntityQuery.Parse<Order>("?filter=total:gt(100)");

        Assert.That(query.FilterModel, Is.Not.Null);
        Assert.That(query.Filter, Is.Not.Null);

        var json = query.FilterModel!.ToJson(Verify.Serializer());
        Assert.That(json, Does.Contain("GreaterThan"));
    }

    // ---------------------------------------------------------------- errors

    [Test]
    public void Syntax_errors_are_reported_with_position()
    {
        Assert.Throws<QueryStringParseException>(() => QueryFilter.Parse<Order>("total:zz(1)"));
        Assert.Throws<QueryStringParseException>(() => QueryFilter.Parse<Order>("and(total:gt(1)"));
        Assert.Throws<QueryStringParseException>(() => QueryFilter.Parse<Order>("id:1)"));
        Assert.Throws<QueryStringParseException>(() => QueryFilter.Parse<Order>("status:in()"));
        Assert.Throws<QueryStringParseException>(() => QuerySort<Order>.Parse("total:sideways"));
        Assert.Throws<QueryStringParseException>(() => EntityQuery.Parse<Order>("?filter=id:1&skip=abc"));

        var positioned = Assert.Throws<QueryStringParseException>(() => QueryFilter.Parse<Order>("total:zz(1)"))!;
        Assert.That(positioned.Position, Is.GreaterThan(0));
        Assert.That(positioned.Message, Does.Contain("zz"));
    }

    [Test]
    public void Unknown_members_fail_through_engine_resolution()
    {
        Assert.Throws<TypeResolutionException>(() => QueryFilter.Parse<Order>("ghost:eq(1)"));
    }

    [Test]
    public void Incompatible_values_fail_with_engine_error()
    {
        Assert.Throws<ExpressionSerializationException>(() => QueryFilter.Parse<Order>("total:gt(not-a-number)"));
    }
}
