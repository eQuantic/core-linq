using eQuantic.Linq.Expressions.Tests.TestModel;
using eQuantic.Linq.Web;

namespace eQuantic.Linq.Expressions.Tests;

[TestFixture]
public class QuerySortBuilderTests
{
    [Test]
    public void Builds_ordering_string_code_first()
    {
        var orderBy = QuerySortBuilder.For<Order>()
            .OrderByDescending(o => o.Total)
            .ThenBy(o => o.Customer.Name)
            .ToString();

        Assert.That(orderBy, Is.EqualTo("total:desc,customer.name"));
    }

    [Test]
    public void Ascending_direction_is_omitted()
    {
        Assert.That(QuerySortBuilder.For<Order>().OrderBy(o => o.Id).ToString(), Is.EqualTo("id"));
    }

    [Test]
    public void ToSorts_feeds_the_typed_sort_surface()
    {
        var sorts = QuerySortBuilder.For<Order>().OrderByDescending(o => o.Total).ToSorts();

        Assert.Multiple(() =>
        {
            Assert.That(sorts, Has.Count.EqualTo(1));
            Assert.That(sorts[0].Path, Is.EqualTo("total"));
            Assert.That(sorts[0].Direction, Is.EqualTo(SortDirection.Descending));
            Assert.That(sorts[0].KeySelector, Is.Not.Null);
        });
    }

    [Test]
    public void Round_trips_through_parse()
    {
        const string orderBy = "total:desc,customer.name";
        Assert.That(QuerySortBuilder.Parse<Order>(orderBy).ToString(), Is.EqualTo(orderBy));
    }
}

[TestFixture]
public class QueryFilterBuilderTests
{
    [Test]
    public void Builds_flat_and_of_comparisons()
    {
        var filter = QueryFilterBuilder.For<Order>()
            .Where(o => o.Total, FilterOperator.GreaterThan, 100m)
            .Where(o => o.Status, FilterOperator.Equal, OrderStatus.Paid)
            .ToString();

        Assert.That(filter, Is.EqualTo("total:gt(100),status:eq(Paid)"));
    }

    [Test]
    public void Builds_navigation_paths_and_string_operators()
    {
        var filter = QueryFilterBuilder.For<Order>()
            .Where(o => o.Customer.Name, FilterOperator.Contains, "li")
            .ToString();

        Assert.That(filter, Is.EqualTo("customer.name:ct(li)"));
    }

    [Test]
    public void Builds_null_tests_and_membership()
    {
        Assert.Multiple(() =>
        {
            Assert.That(QueryFilterBuilder.For<Order>().WhereNull(o => o.Notes).ToString(),
                Is.EqualTo("notes:eq(null)"));
            Assert.That(QueryFilterBuilder.For<Order>().WhereNotNull(o => o.DeliveredAt).ToString(),
                Is.EqualTo("deliveredAt:neq(null)"));
            Assert.That(QueryFilterBuilder.For<Order>().WhereIn(o => o.Status, OrderStatus.Paid, OrderStatus.Shipped).ToString(),
                Is.EqualTo("status:in(Paid|Shipped)"));
        });
    }

    [Test]
    public void Flat_and_or_chain_folds_left_to_right()
    {
        // (total > 100 AND status == Paid) OR isVip
        var filter = QueryFilterBuilder.For<Order>()
            .Where(o => o.Total, FilterOperator.GreaterThan, 100m)
            .And(o => o.Status, FilterOperator.Equal, OrderStatus.Paid)
            .Or(o => o.Customer.IsVip, FilterOperator.Equal, true)
            .ToString();

        Assert.That(filter, Is.EqualTo("or(and(total:gt(100),status:eq(Paid)),customer.isVip:eq(true))"));
    }

    [Test]
    public void Pure_and_chain_flattens_to_a_comma_list()
    {
        var filter = QueryFilterBuilder.For<Order>()
            .Where(o => o.Total, FilterOperator.GreaterThan, 100m)
            .And(o => o.Status, FilterOperator.Equal, OrderStatus.Paid)
            .And(o => o.Customer.IsVip, FilterOperator.Equal, true)
            .ToString();

        Assert.That(filter, Is.EqualTo("total:gt(100),status:eq(Paid),customer.isVip:eq(true)"));
    }

    [Test]
    public void Pure_or_chain_flattens_to_one_or_group()
    {
        var filter = QueryFilterBuilder.For<Order>()
            .Where(o => o.Status, FilterOperator.Equal, OrderStatus.Paid)
            .Or(o => o.Status, FilterOperator.Equal, OrderStatus.Shipped)
            .Or(o => o.Status, FilterOperator.Equal, OrderStatus.Delivered)
            .ToString();

        Assert.That(filter, Is.EqualTo("or(status:eq(Paid),status:eq(Shipped),status:eq(Delivered))"));
    }

    [Test]
    public void And_group_nests_an_or_and_not()
    {
        // total > 100 AND (status == Paid OR isVip) AND NOT cancelled
        var filter = QueryFilterBuilder.For<Order>()
            .Where(o => o.Total, FilterOperator.GreaterThan, 100m)
            .And(g => g
                .Where(o => o.Status, FilterOperator.Equal, OrderStatus.Paid)
                .Or(o => o.Customer.IsVip, FilterOperator.Equal, true))
            .Not(g => g.Where(o => o.Status, FilterOperator.Equal, OrderStatus.Cancelled))
            .ToString();

        Assert.That(filter, Is.EqualTo("total:gt(100),or(status:eq(Paid),customer.isVip:eq(true)),not(status:eq(Cancelled))"));
    }

    [Test]
    public void Quotes_values_that_carry_grammar_meaning()
    {
        var filter = QueryFilterBuilder.For<Order>()
            .Where(o => o.Notes, FilterOperator.Contains, "Leave at (door), please")
            .ToString();

        Assert.That(filter, Is.EqualTo("notes:ct('Leave at (door), please')"));
    }

    [Test]
    public void Built_filter_executes_through_the_engine()
    {
        var predicate = QueryFilterBuilder.For<Order>()
            .Where(o => o.Total, FilterOperator.GreaterThan, 100m)
            .Where(o => o.Customer.Name, FilterOperator.Contains, "li")
            .ToPredicate()
            .Compile();

        Assert.Multiple(() =>
        {
            Assert.That(predicate(new Order { Total = 150, Customer = new Customer { Name = "Alice" } }), Is.True);
            Assert.That(predicate(new Order { Total = 50, Customer = new Customer { Name = "Alice" } }), Is.False);
            Assert.That(predicate(new Order { Total = 150, Customer = new Customer { Name = "Bob" } }), Is.False);
        });
    }

    [Test]
    public void Round_trips_through_parse()
    {
        const string filter = "total:gt(100),or(status:eq(Paid),customer.isVip:eq(true)),not(notes:eq(null))";
        Assert.That(QueryFilterBuilder.Parse<Order>(filter).ToString(), Is.EqualTo(filter));
    }

    [Test]
    public void Parse_reads_shorthand_membership_and_requotes_values()
    {
        Assert.Multiple(() =>
        {
            Assert.That(QueryFilterBuilder.Parse<Order>("id:3").ToString(), Is.EqualTo("id:eq(3)"));
            Assert.That(QueryFilterBuilder.Parse<Order>("status:in(Paid|Shipped)").ToString(), Is.EqualTo("status:in(Paid|Shipped)"));
            Assert.That(QueryFilterBuilder.Parse<Order>("notes:ct('a,b')").ToString(), Is.EqualTo("notes:ct('a,b')"));
        });
    }

    [Test]
    public void Parse_can_be_inspected_and_extended()
    {
        var predicate = QueryFilterBuilder.Parse<Order>("total:gt(100)")
            .Where(o => o.Status, FilterOperator.Equal, OrderStatus.Paid)
            .ToPredicate()
            .Compile();

        Assert.Multiple(() =>
        {
            Assert.That(predicate(new Order { Total = 150, Status = OrderStatus.Paid }), Is.True);
            Assert.That(predicate(new Order { Total = 150, Status = OrderStatus.New }), Is.False);
        });
    }

    [Test]
    public void Parse_rejects_constructs_it_cannot_model()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => QueryFilterBuilder.Parse<Order>("items:any(price:gt(50))"), Throws.InstanceOf<QueryStringParseException>());
            Assert.That(() => QueryFilterBuilder.Parse<Order>("items.count():gt(1)"), Throws.InstanceOf<QueryStringParseException>());
        });
    }

    [Test]
    public void String_path_overloads_mirror_the_lambda_form()
    {
        var lambda = QueryFilterBuilder.For<Order>()
            .Where(o => o.Total, FilterOperator.GreaterThan, 100m)
            .And(o => o.Customer.Name, FilterOperator.Contains, "li")
            .ToString();

        var strings = QueryFilterBuilder.For<Order>()
            .Where("total", FilterOperator.GreaterThan, 100m)
            .And("customer.name", FilterOperator.Contains, "li")
            .ToString();

        Assert.That(strings, Is.EqualTo(lambda));
    }

    [Test]
    public void String_paths_can_express_segments_beyond_lambdas()
    {
        var filter = QueryFilterBuilder.For<Order>()
            .Where("items.count()", FilterOperator.GreaterThan, 1)
            .ToString();

        Assert.That(filter, Is.EqualTo("items.count():gt(1)"));
    }

    [Test]
    public void And_clause_is_sugar_for_where()
    {
        var filter = QueryFilterBuilder.For<Order>()
            .Where(o => o.Total, FilterOperator.GreaterThan, 100m)
            .And(o => o.Status, FilterOperator.Equal, OrderStatus.Paid)
            .ToString();

        Assert.That(filter, Is.EqualTo("total:gt(100),status:eq(Paid)"));
    }

    [Test]
    public void Sort_builder_accepts_string_paths()
    {
        Assert.That(QuerySortBuilder.For<Order>().OrderByDescending("total").ThenBy("customer.name").ToString(),
            Is.EqualTo("total:desc,customer.name"));
    }

    [Test]
    public void Flat_string_chain_folds_like_the_typed_form()
    {
        var filter = QueryFilterBuilder.For<Order>()
            .Where("total", FilterOperator.GreaterThan, 100m)
            .And("status", FilterOperator.Equal, "Paid")
            .Or("customer.isVip", FilterOperator.Equal, true)
            .ToString();

        Assert.That(filter, Is.EqualTo("or(and(total:gt(100),status:eq(Paid)),customer.isVip:eq(true))"));
    }
}
