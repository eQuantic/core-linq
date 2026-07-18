using System.Linq.Expressions;
using eQuantic.Linq.Expressions.Tests.Support;
using eQuantic.Linq.Expressions.Tests.TestModel;

namespace eQuantic.Linq.Expressions.Tests;

/// <summary>The curated v3 helper-extension surface of eQuantic.Linq.Expressions.</summary>
[TestFixture]
public class HelperExtensionTests
{
    private static List<int> Ids(IQueryable<Order> q) => q.Select(o => o.Id).ToList();

    [Test]
    public void Predicate_builder_composes_dynamic_filters()
    {
        // classic accumulation pattern: OR-fold over user choices starting from False
        var statuses = new[] { OrderStatus.Paid, OrderStatus.Shipped };
        var predicate = PredicateBuilder.False<Order>();
        foreach (var status in statuses)
        {
            predicate = predicate.OrElse(o => o.Status == status);
        }

        predicate = predicate.AndAlso(o => o.Total > 100m);

        Assert.That(Ids(TestData.OrdersQuery.Where(predicate)), Is.EqualTo(new[] { 1, 3, 6 }));
        Assert.That(Ids(TestData.OrdersQuery.Where(PredicateBuilder.True<Order>().Not())), Is.Empty);
    }

    [Test]
    public void Composition_keeps_a_single_parameter()
    {
        Expression<Func<Order, bool>> composed = PredicateBuilder.True<Order>()
            .AndAlso(o => o.Total > 0m)
            .OrElse(o => o.Customer.IsVip);

        var parameters = new HashSet<ParameterExpression>();
        new Collector(parameters).Visit(composed.Body);

        Assert.That(parameters.Single(), Is.SameAs(composed.Parameters[0]));
    }

    private sealed class Collector(HashSet<ParameterExpression> parameters) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            parameters.Add(node);
            return base.VisitParameter(node);
        }
    }

    [Test]
    public void Member_path_extensions_extract_and_rebuild_paths()
    {
        Expression<Func<Order, object>> selector = o => o.Customer.Name;

        Assert.That(selector.GetMemberPath(), Is.EqualTo("Customer.Name"));
        Assert.That(selector.GetMemberName(), Is.EqualTo("Name"));
        Assert.That(selector.GetMember().Name, Is.EqualTo("Name"));

        // [Column("customer_age")] on Customer.Age
        Expression<Func<Order, int>> age = o => o.Customer.Age;
        Assert.That(age.GetMemberPath(columnFallback: true), Is.EqualTo("Customer.customer_age"));
        Assert.That(age.GetMemberName(columnFallback: true), Is.EqualTo("customer_age"));

        // reverse: dotted path → typed selector, through engine inference (camelCase ok)
        var rebuilt = MemberPathExtensions.ToSelector<Order>("customer.name");
        Assert.That(rebuilt.ReturnType, Is.EqualTo(typeof(string)));
        Assert.That(((Func<Order, string>)rebuilt.Compile())(TestData.Orders[0]), Is.EqualTo("Alice"));

        Assert.Throws<ArgumentException>(() => ((Expression<Func<Order, int>>)(o => o.Id + 1)).GetMemberPath());
    }

    [Test]
    public void Queryable_applies_models_and_json_directly()
    {
        var serializer = Verify.Serializer();
        var model = serializer.ToModel<Order, bool>(o => o.Items.Any(i => i.Price > 500m));

        Assert.That(Ids(TestData.OrdersQuery.Where(model, serializer)), Is.EqualTo(new[] { 3 }));

        var json = serializer.ToJson(model);
        Assert.That(Ids(TestData.OrdersQuery.WhereJson(json, serializer)), Is.EqualTo(new[] { 3 }));
    }

    [Test]
    public void Queryable_orders_by_dotted_paths()
    {
        var ordered = TestData.OrdersQuery
            .OrderByPath("customer.name", descending: true)
            .ThenByPath("total", descending: true)
            .ThenByPath("id");

        Assert.That(Ids(ordered), Is.EqualTo(Ids(TestData.OrdersQuery
            .OrderByDescending(o => o.Customer.Name)
            .ThenByDescending(o => o.Total)
            .ThenBy(o => o.Id))));
    }

    [Test]
    public void Null_guard_sugar_is_available_on_predicates()
    {
        Expression<Func<Order, bool>> raw = o => o.Customer.Address!.City == "Lisboa";

        Assert.That(Ids(TestData.OrdersQuery.Where(raw.WithNullGuards())), Is.EqualTo(new[] { 1, 4 }));
    }
}
