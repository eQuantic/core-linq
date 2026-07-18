using System.Linq.Expressions;
using eQuantic.Linq.Specification;
using eQuantic.Linq.Expressions.Tests.Support;
using eQuantic.Linq.Expressions.Tests.TestModel;
using eQuantic.Linq.Web.Specification;

// Inside the eQuantic.Linq.* namespace tree the identifier "Specification" binds to the ancestor
// namespace before usings are considered; a closed alias keeps the tests readable.
using OrderSpecification = eQuantic.Linq.Specification.Specification<eQuantic.Linq.Expressions.Tests.TestModel.Order>;

namespace eQuantic.Linq.Expressions.Tests;

/// <summary>
/// The specification pattern ported to v3: classic composition semantics preserved, plus the two new
/// entry points — serialized expression models and REST filter query strings.
/// </summary>
[TestFixture]
public class SpecificationTests
{
    private static List<int> Ids(ISpecification<Order> specification) =>
        TestData.OrdersQuery.Where(specification.SatisfiedBy()).Select(o => o.Id).ToList();

    private static readonly OrderSpecification Paid =
        new DirectSpecification<Order>(o => o.Status == OrderStatus.Paid);

    private static readonly OrderSpecification Big =
        new DirectSpecification<Order>(o => o.Total > 300m);

    private static readonly OrderSpecification Vip =
        new DirectSpecification<Order>(o => o.Customer.IsVip);

    [Test]
    public void Direct_and_true_specifications()
    {
        Assert.That(Ids(Paid), Is.EqualTo(new[] { 1, 6 }));
        Assert.That(Ids(new TrueSpecification<Order>()), Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6 }));
    }

    [Test]
    public void Composition_methods_match_linq_semantics()
    {
        Assert.That(Ids(Paid.And(Vip)), Is.EqualTo(Ids(new DirectSpecification<Order>(o => o.Status == OrderStatus.Paid && o.Customer.IsVip))));
        Assert.That(Ids(Paid.AndAlso(Vip)), Is.EqualTo(new[] { 1, 6 }));
        Assert.That(Ids(Paid.Or(Big)), Is.EqualTo(new[] { 1, 3, 5, 6 }));
        Assert.That(Ids(Paid.OrElse(Big)), Is.EqualTo(new[] { 1, 3, 5, 6 }));
        Assert.That(Ids(Paid.Not()), Is.EqualTo(new[] { 2, 3, 4, 5 }));
        Assert.That(Ids(Paid.AndNot(Vip)), Is.Empty);
        Assert.That(Ids(Big.OrNot(Vip)), Is.EqualTo(new[] { 2, 3, 5 }));
    }

    [Test]
    public void Operator_overloads_compose()
    {
        Assert.That(Ids(Paid & Vip), Is.EqualTo(new[] { 1, 6 }));
        Assert.That(Ids(Paid | Big), Is.EqualTo(new[] { 1, 3, 5, 6 }));
        Assert.That(Ids(!Paid), Is.EqualTo(new[] { 2, 3, 4, 5 }));
        Assert.That(Ids((Paid & Vip) | !Big), Is.EqualTo(new[] { 1, 2, 4, 6 }));
    }

    [Test]
    public void Composed_expressions_share_a_single_parameter()
    {
        // ParameterRebinder must merge parameters (no Invoke) so providers can translate the tree.
        var predicate = (Paid & Vip).SatisfiedBy();

        var parameters = new HashSet<ParameterExpression>();
        new ParameterCollector(parameters).Visit(predicate.Body);

        Assert.That(parameters, Has.Count.EqualTo(1));
        Assert.That(parameters.Single(), Is.SameAs(predicate.Parameters[0]));
    }

    private sealed class ParameterCollector(HashSet<ParameterExpression> parameters) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            parameters.Add(node);
            return base.VisitParameter(node);
        }
    }

    [Test]
    public void Expression_builder_extensions_compose_expressions()
    {
        Expression<Func<Order, bool>> paid = o => o.Status == OrderStatus.Paid;
        Expression<Func<Order, bool>> vip = o => o.Customer.IsVip;

        Assert.That(TestData.OrdersQuery.Where(paid.AndAlso(vip)).Select(o => o.Id), Is.EqualTo(new[] { 1, 6 }));
        Assert.That(TestData.OrdersQuery.Where(paid.OrElse(vip)).Select(o => o.Id), Is.EqualTo(new[] { 1, 3, 4, 6 }));
    }

    [Test]
    public void Expression_model_specification_from_model()
    {
        var serializer = Verify.Serializer();
        var model = serializer.ToModel<Order, bool>(o => o.Items.Any(i => i.Price > 500m));

        var specification = new ExpressionModelSpecification<Order>(model, serializer);

        Assert.That(Ids(specification), Is.EqualTo(new[] { 3 }));
        Assert.That(specification.Model, Is.SameAs(model));
    }

    [Test]
    public void Expression_model_specification_from_json_payload()
    {
        const string json = """
        {
          "body": {
            "$type": "binary",
            "nodeType": "GreaterThan",
            "left": { "$type": "member", "member": { "name": "total" }, "expression": { "$type": "parameter" } },
            "right": { "$type": "constant", "value": 300 }
          }
        }
        """;

        var specification = new ExpressionModelSpecification<Order>(json, Verify.Serializer());

        Assert.That(Ids(specification), Is.EqualTo(new[] { 3, 5 }));
    }

    [Test]
    public void Expression_model_specification_composes_with_others()
    {
        var fromWire = new ExpressionModelSpecification<Order>(
            Verify.Serializer().ToModel<Order, bool>(o => o.Total > 100m),
            Verify.Serializer());

        var combined = fromWire.AndAlso(new DirectSpecification<Order>(o => o.Status != OrderStatus.Cancelled));

        Assert.That(Ids(combined), Is.EqualTo(new[] { 1, 3, 6 }));
    }

    [Test]
    public void Query_string_specification_parses_the_rest_filter_syntax()
    {
        var specification = new QueryStringSpecification<Order>("status:in(Paid|Shipped),items.sum(price):gt(100)");

        Assert.That(Ids(specification), Is.EqualTo(new[] { 1, 3, 6 }));
    }

    [Test]
    public void Query_string_specification_composes_with_domain_rules()
    {
        var fromClient = new QueryStringSpecification<Order>("total:gt(100)");
        var notCancelled = new DirectSpecification<Order>(o => o.Status != OrderStatus.Cancelled);

        Assert.That(Ids(fromClient.AndAlso(notCancelled)), Is.EqualTo(new[] { 1, 3, 6 }));
        Assert.That(Ids((OrderSpecification)fromClient & notCancelled), Is.EqualTo(new[] { 1, 3, 6 }));
    }

    [Test]
    public void Query_string_specification_exposes_transportable_model()
    {
        var specification = new QueryStringSpecification<Order>("items:any(price:gt(500))");

        var json = Verify.Serializer().ToJson(specification.Model);
        var roundTripped = new ExpressionModelSpecification<Order>(json, Verify.Serializer());

        Assert.That(Ids(roundTripped), Is.EqualTo(Ids(specification)));
    }

    [Test]
    public void Query_string_specification_fails_eagerly_on_invalid_syntax()
    {
        Assert.Throws<eQuantic.Linq.Web.QueryStringParseException>(() => _ = new QueryStringSpecification<Order>("total:zz(1)"));
        Assert.Throws<ArgumentException>(() => _ = new QueryStringSpecification<Order>("  "));
    }
}
