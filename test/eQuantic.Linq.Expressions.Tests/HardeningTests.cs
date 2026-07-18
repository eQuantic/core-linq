using System.Globalization;
using System.Linq.Expressions;
using eQuantic.Linq.Expressions.Metadata;
using eQuantic.Linq.Expressions.Nodes;
using eQuantic.Linq.Expressions.Tests.Support;
using eQuantic.Linq.Expressions.Tests.TestModel;
using eQuantic.Linq.Web;

namespace eQuantic.Linq.Expressions.Tests;

/// <summary>Hardening round: depth/size limits, wide filters, membership via Contains, method gating and the secure factory.</summary>
[TestFixture]
public class HardeningTests
{
    private static List<int> Ids(IQueryable<Order> query) => query.Select(o => o.Id).ToList();

    [Test]
    public void Wide_filters_survive_json_depth_limits()
    {
        // 40 AND terms + 40-value membership: the old left-fold + OrElse-chain shapes would
        // blow past System.Text.Json's default MaxDepth of 64.
        var terms = string.Join(",", Enumerable.Range(0, 40).Select(_ => "total:gt(0)"));
        var membership = "id:in(" + string.Join("|", Enumerable.Range(1, 40)) + ")";

        var predicate = QueryFilter.Parse<Order>($"{terms},{membership}");

        Assert.That(Ids(TestData.OrdersQuery.Where(predicate)), Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6 }));

        // and the model round-trips through JSON
        var serializer = Verify.Serializer();
        var json = serializer.ToJson(QueryFilter.ParseModel<Order>($"{terms},{membership}"));
        var rebuilt = serializer.ModelFromJson<Order>(json).ToPredicate(serializer);

        Assert.That(Ids(TestData.OrdersQuery.Where(rebuilt)), Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6 }));
    }

    [Test]
    public void Membership_uses_contains_over_a_typed_array()
    {
        var predicate = QueryFilter.Parse<Order>("status:in(Paid|Shipped)");
        Assert.That(Ids(TestData.OrdersQuery.Where(predicate)), Is.EqualTo(new[] { 1, 3, 6 }));

        // shape: Enumerable.Contains(OrderStatus[], o.Status) — flat, SQL-IN friendly
        var call = (MethodCallExpression)predicate.Body;
        Assert.That(call.Method.Name, Is.EqualTo("Contains"));
        Assert.That(call.Arguments[0], Is.InstanceOf<ConstantExpression>());
        Assert.That(((ConstantExpression)call.Arguments[0]).Value, Is.InstanceOf<OrderStatus[]>());

        var negative = QueryFilter.Parse<Order>("id:nin(1|2|3)");
        Assert.That(Ids(TestData.OrdersQuery.Where(negative)), Is.EqualTo(new[] { 4, 5, 6 }));
    }

    [Test]
    public void Decoder_enforces_depth_and_node_limits()
    {
        Expression body = Expression.Constant(true);
        for (var i = 0; i < 200; i++)
        {
            body = Expression.Not(body);
        }

        var lambda = Expression.Lambda<Func<bool>>(body);
        var permissive = Verify.Serializer(o => o.EnablePartialEvaluation = false);
        var json = permissive.ToJson(lambda);

        var tightDepth = Verify.Serializer(o => o.MaxDepth = 50);
        var depthError = Assert.Throws<ExpressionSerializationException>(() => tightDepth.FromJson(json));
        Assert.That(depthError!.Message, Does.Contain("MaxDepth"));

        var tightNodes = Verify.Serializer(o => o.MaxNodes = 20);
        var nodesError = Assert.Throws<ExpressionSerializationException>(() => tightNodes.FromJson(json));
        Assert.That(nodesError!.Message, Does.Contain("MaxNodes"));
    }

    [Test]
    public void Parser_enforces_nesting_limits()
    {
        var nested = string.Concat(Enumerable.Repeat("not(", 80)) + "id:1" + new string(')', 80);

        Assert.Throws<QueryStringParseException>(() => QueryFilter.Parse<Order>(nested));
    }

    [Test]
    public void Method_filter_gates_every_resolved_method()
    {
        Expression<Func<Order, bool>> lambda = o => StaticHelpers.Twice(o.Id) > 4;
        var json = Verify.Serializer().ToJson(lambda);

        var gated = Verify.Serializer(o => o.MethodFilter = m => m.DeclaringType != typeof(StaticHelpers));
        var error = Assert.Throws<ExpressionSerializationException>(() => gated.FromJson(json));

        Assert.That(error!.Message, Does.Contain("MethodFilter"));
        Assert.That(error.Message, Does.Contain("Twice"));

        // same tree passes with the filter open
        var open = Verify.Serializer(o => o.MethodFilter = _ => true);
        Assert.That(Ids(TestData.OrdersQuery.Where(open.FromJson<Func<Order, bool>>(json))), Is.EqualTo(new[] { 3, 4, 5, 6 }));
    }

    [Test]
    public void Secure_serializer_allows_contract_queries_and_blocks_the_rest()
    {
        var secure = ExpressionSerializer.CreateSecure(typeof(Order), typeof(OrderItem), typeof(Customer));
        secure.Options.QueryRootProvider = TestData.GetQueryable;

        var options = new QueryStringOptions { Serializer = secure };

        // realistic filter: navigation, aggregate, string method, membership
        var predicate = QueryFilter.Parse<Order>(
            "and(customer.name.toUpper():sw(CAR),items.sum(price):gt(100),status:in(Paid|Shipped))",
            options);
        Assert.That(Ids(TestData.OrdersQuery.Where(predicate)), Is.EqualTo(new[] { 3, 6 }));

        // unregistered types are blocked
        var foreignJson = Verify.Serializer().ToJson((Expression<Func<Money, decimal>>)(m => m.Amount));
        Assert.Throws<TypeResolutionException>(() => secure.FromJson(foreignJson));

        // non-allowlisted static holders are blocked (strict typing catches them even before the method gate)
        var gatedJson = Verify.Serializer().ToJson((Expression<Func<Order, bool>>)(o => StaticHelpers.Twice(o.Id) > 4));
        Assert.Catch<ExpressionSerializationException>(() => secure.FromJson(gatedJson));
    }

    [Test]
    public void Query_string_options_expose_strict_sugar()
    {
        var options = new QueryStringOptions().UseStrictSerializer(typeof(Order), typeof(OrderItem), typeof(Customer));

        var predicate = QueryFilter.Parse<Order>("total:gt(100),items:any(price:gt(50))", options);
        Assert.That(Ids(TestData.OrdersQuery.Where(predicate)), Is.EqualTo(new[] { 1, 3, 6 }));
    }

    [Test]
    public void Anonymous_emission_cap_is_enforced()
    {
        var resolver = new Resolution.DefaultTypeResolver(new Resolution.TypeResolutionOptions
        {
            MaxAnonymousTypes = 0,
        });

        var freshShape = new TypeRef
        {
            IsAnonymous = true,
            Properties = [new AnonymousTypeProperty("NeverEmittedBefore" + nameof(HardeningTests), new TypeRef("int"))],
        };

        var error = Assert.Throws<TypeResolutionException>(() => resolver.ResolveType(freshShape));
        Assert.That(error!.Message, Does.Contain("cap"));

        var disabled = new Resolution.DefaultTypeResolver(new Resolution.TypeResolutionOptions
        {
            AllowAnonymousTypes = false,
        });
        Assert.Throws<TypeResolutionException>(() => disabled.ResolveType(freshShape));
    }

    [Test]
    public void Format_provider_governs_string_coercion()
    {
        var brazilian = Verify.Serializer(o => o.FormatProvider = new CultureInfo("pt-BR"));
        var options = new QueryStringOptions { Serializer = brazilian };

        var predicate = QueryFilter.Parse<Order>("total:gt('300,5')", options);
        Assert.That(Ids(TestData.OrdersQuery.Where(predicate)), Is.EqualTo(new[] { 3, 5 }));

        // under the invariant default the comma reads as a THOUSANDS separator → 3005 → no matches
        var invariant = QueryFilter.Parse<Order>("total:gt('300,5')");
        Assert.That(Ids(TestData.OrdersQuery.Where(invariant)), Is.Empty);
    }
}
