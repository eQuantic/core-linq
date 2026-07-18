using System.Linq.Expressions;
using eQuantic.Linq.Expressions.Casting;
using eQuantic.Linq.Expressions.Tests.Support;
using eQuantic.Linq.Expressions.Tests.TestModel;
using eQuantic.Linq.Web;

namespace eQuantic.Linq.Expressions.Tests;

/// <summary>
/// DTO → entity expression casting: filters authored over the API shape are rewritten onto the data
/// model — flattened navigations, computed maps (math, concatenation, aggregates), nested collection
/// shapes with generic re-binding, and column fallback.
/// </summary>
[TestFixture]
public class ExpressionCastTests
{
    private static readonly ExpressionCast<OrderDto, Order> Cast = ExpressionCast.Create<OrderDto, Order>(options => options
        .Map(d => d.CustomerName, e => e.Customer.Name)
        .Map(d => d.Revenue, e => e.Items.Sum(i => i.Price * i.Quantity))
        .Map(d => d.Display, e => e.Customer.Name + " #" + e.Id)
        .Map(d => d.StatusName, e => e.Status.ToString())
        .Nested<ItemDto, OrderItem>(nested => nested.Map(i => i.Cost, e => e.Price)));

    private static List<int> Ids(IQueryable<Order> query) => query.Select(o => o.Id).ToList();

    private static void AssertCast(Expression<Func<OrderDto, bool>> dtoPredicate, Expression<Func<Order, bool>> mirror)
    {
        var cast = Cast.Predicate(dtoPredicate);

        var actual = Ids(TestData.OrdersQuery.Where(cast));
        var expected = Ids(TestData.OrdersQuery.Where(mirror));

        Assert.That(actual, Is.EqualTo(expected), $"dto predicate: {dtoPredicate}");
    }

    [Test]
    public void Members_map_automatically_by_name()
    {
        AssertCast(d => d.Id == 3, o => o.Id == 3);
        AssertCast(d => d.Total > 300m, o => o.Total > 300m);
        AssertCast(d => d.Status == OrderStatus.Paid, o => o.Status == OrderStatus.Paid);
    }

    [Test]
    public void Flattened_navigation_maps()
    {
        AssertCast(d => d.CustomerName.Contains("li"), o => o.Customer.Name.Contains("li"));
        AssertCast(d => d.CustomerName.ToUpper().StartsWith("CAR"), o => o.Customer.Name.ToUpper().StartsWith("CAR"));
    }

    [Test]
    public void Computed_math_map()
    {
        AssertCast(d => d.Revenue > 200m, o => o.Items.Sum(i => i.Price * i.Quantity) > 200m);
        AssertCast(d => d.Revenue >= 250.50m && d.Revenue < 1000m,
            o => o.Items.Sum(i => i.Price * i.Quantity) >= 250.50m && o.Items.Sum(i => i.Price * i.Quantity) < 1000m);
    }

    [Test]
    public void Concatenation_map()
    {
        AssertCast(d => d.Display.EndsWith("#3"), o => (o.Customer.Name + " #" + o.Id).EndsWith("#3"));
        AssertCast(d => d.Display == "Alice #1", o => o.Customer.Name + " #" + o.Id == "Alice #1");
    }

    [Test]
    public void Enum_to_string_map()
    {
        AssertCast(d => d.StatusName == "Paid", o => o.Status.ToString() == "Paid");
    }

    [Test]
    public void Nested_collections_rebind_generic_methods()
    {
        AssertCast(d => d.Items.Any(i => i.Cost > 500m), o => o.Items.Any(i => i.Price > 500m));
        AssertCast(d => d.Items.Any(i => i.Product == "Mouse"), o => o.Items.Any(i => i.Product == "Mouse"));
        AssertCast(d => d.Items.Sum(i => i.Cost) > 200m, o => o.Items.Sum(i => i.Price) > 200m);
        AssertCast(
            d => d.Items.Count(i => i.Cost > 100m && i.Quantity >= 1) >= 1,
            o => o.Items.Count(i => i.Price > 100m && i.Quantity >= 1) >= 1);
    }

    [Test]
    public void Column_fallback_maps_source_members_by_column_name()
    {
        // OrderDto.Amount carries [Column("Total")] → resolves to Order.Total.
        AssertCast(d => d.Amount > 300m, o => o.Total > 300m);
    }

    [Test]
    public void Column_fallback_can_be_disabled()
    {
        var strict = ExpressionCast.Create<OrderDto, Order>(options => options.ColumnFallback = false);

        var exception = Assert.Throws<ExpressionCastException>(() => strict.Predicate(d => d.Amount > 300m));
        Assert.That(exception!.Message, Does.Contain("Amount"));
    }

    [Test]
    public void Engine_resolves_entity_members_by_column_attribute()
    {
        // No cast involved: the query-string path uses the [Column("customer_age")] name directly.
        var actual = Ids(TestData.OrdersQuery.WhereQueryString("customer.customer_age:gte(34)"));
        var expected = Ids(TestData.OrdersQuery.Where(o => o.Customer.Age >= 34));

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Unmapped_member_fails_with_actionable_error()
    {
        var exception = Assert.Throws<ExpressionCastException>(() => Cast.Predicate(d => d.Legacy == "x"));

        Assert.That(exception!.Message, Does.Contain("Legacy"));
        Assert.That(exception.Message, Does.Contain("Map"));
    }

    [Test]
    public void Incompatible_auto_map_fails_with_actionable_error()
    {
        // OrderDto.Priority is string; Order.Priority is an int field.
        var exception = Assert.Throws<ExpressionCastException>(() => Cast.Predicate(d => d.Priority == "2"));

        Assert.That(exception!.Message, Does.Contain("Map"));
    }

    [Test]
    public void Sorting_and_selection_cast_through_entity_query()
    {
        var query = EntityQuery
            .Parse<OrderDto>("?filter=revenue:gt(200),customerName:ct(a)&orderBy=revenue:desc&select=id,customerName,revenue")
            .Cast(Cast);

        var results = query.ApplyWithSelection(TestData.OrdersQuery).Cast<object>().ToList();

        Assert.That(results.Select(r => r.ToString()), Is.EqualTo(new[]
        {
            "{ Id = 3, CustomerName = Carla, Revenue = 1899.00 }",
            "{ Id = 6, CustomerName = Carla, Revenue = 250.50 }",
        }));
    }

    [Test]
    public void Paging_survives_the_cast()
    {
        var query = EntityQuery
            .Parse<OrderDto>("?filter=status:neq(Cancelled)&orderBy=total:desc&skip=1&take=2")
            .Cast(Cast);

        var actual = Ids(query.Apply(TestData.OrdersQuery));
        var expected = Ids(TestData.OrdersQuery
            .Where(o => o.Status != OrderStatus.Cancelled)
            .OrderByDescending(o => o.Total)
            .Skip(1)
            .Take(2));

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Cast_query_exposes_entity_filter_model()
    {
        var query = EntityQuery.Parse<OrderDto>("?filter=revenue:gt(200)").Cast(Cast);

        Assert.That(query.FilterModel, Is.Not.Null);

        var json = Verify.Serializer().ToJson(query.FilterModel!);
        Assert.That(json, Does.Contain("\"Sum\""));
        Assert.That(json, Does.Contain("Price"));
        Assert.That(json, Does.Not.Contain("Revenue"), "the entity-side model no longer references DTO members");
    }

    [Test]
    public void Model_level_cast_bridges_querystring_to_entity_json()
    {
        var serializer = Verify.Serializer();

        var dtoModel = QueryFilter.ParseModel<OrderDto>("revenue:gt(200)");
        var entityModel = Cast.Model(dtoModel, serializer);

        var predicate = entityModel.ToPredicate(serializer);
        Assert.That(Ids(TestData.OrdersQuery.Where(predicate)), Is.EqualTo(new[] { 1, 3, 6 }));
    }

    [Test]
    public void Anonymous_projections_are_rebuilt_when_types_change()
    {
        Expression<Func<OrderDto, object>> projection = d => new { d.Id, d.Items };
        var rewritten = Cast.Lambda(projection);

        var newExpression = rewritten.Body as NewExpression
                            ?? (NewExpression)((UnaryExpression)rewritten.Body).Operand;
        Assert.That(newExpression.Type.GetProperty("Items")!.PropertyType, Is.EqualTo(typeof(List<OrderItem>)));

        var project = rewritten.Compile();
        var result = project.DynamicInvoke(TestData.Orders[0]);
        Assert.That(result!.ToString(), Does.StartWith("{ Id = 1, Items ="));
    }

    [Test]
    public void Selector_cast_preserves_result_type()
    {
        var selector = Cast.Selector<decimal>(d => d.Revenue);

        Assert.That(selector.Compile()(TestData.Orders[0]), Is.EqualTo(250.50m));
    }
}
