using System.Linq.Expressions;
using eQuantic.Linq.Expressions.Tests.Support;
using eQuantic.Linq.Expressions.Tests.TestModel;

namespace eQuantic.Linq.Expressions.Tests;

/// <summary>
/// The generic, root-anchored model: <see cref="ExpressionModel{TRoot}"/>. The root type drives
/// top-down inference, so payloads omit parameter types, member owners and contextual constant types.
/// </summary>
[TestFixture]
public class ExpressionModelTests
{
    private static ExpressionSerializer Serializer() => Verify.Serializer();

    private static void AssertPredicateRoundTrip(Expression<Func<Order, bool>> predicate)
    {
        var serializer = Serializer();

        var model = serializer.ToModel(predicate);
        var json = serializer.ToJson(model);

        var recovered = serializer.ModelFromJson<Order>(json);
        var rebuilt = serializer.ToPredicate(recovered);

        // Lean JSON must be idempotent as well.
        var secondJson = serializer.ToJson(serializer.ToModel(rebuilt));
        Assert.That(secondJson, Is.EqualTo(json), $"lean JSON must be idempotent for: {predicate}");

        // The rebuilt tree must be structurally identical to the (partially evaluated) original.
        var prepared = (LambdaExpression)eQuantic.Linq.Expressions.Conversion.PartialEvaluator.Eval(predicate);
        Assert.That(
            Comparison.ExpressionEqualityComparer.Instance.Equals(prepared, rebuilt),
            Is.True,
            $"Structural mismatch.\n original: {prepared}\n rebuilt:  {rebuilt}\n json: {json}");

        var original = predicate.Compile();
        var recoveredPredicate = rebuilt.Compile();
        foreach (var order in TestData.Orders)
        {
            Assert.That(recoveredPredicate(order), Is.EqualTo(original(order)), $"order {order.Id} for: {predicate}");
        }
    }

    [Test]
    public void Predicate_round_trips_with_lean_json()
    {
        AssertPredicateRoundTrip(o => o.Total > 100m);
    }

    [Test]
    public void Lean_json_omits_inferable_type_information()
    {
        var serializer = Serializer();
        var json = serializer.ToJson(serializer.ToModel<Order, bool>(o => o.Total > 100m));

        Assert.That(json, Does.Contain("\"Total\""));
        Assert.That(json, Does.Not.Contain("declaringType"), "member owner is inferable from the root type");
        Assert.That(json, Does.Not.Contain("\"kind\""));
        Assert.That(json, Does.Not.Contain(typeof(Order).FullName!), "the root type itself never appears");
        Assert.That(json, Does.Not.Contain("decimal"), "constant type is inferable from the sibling operand");
        Assert.That(json, Does.Not.Contain("delegateType"));

        TestContext.Out.WriteLine(json);
    }

    [Test]
    public void Complex_predicates_round_trip()
    {
        var minimum = 40m;
        AssertPredicateRoundTrip(o => o.Status == OrderStatus.Paid && o.Total > minimum);
        AssertPredicateRoundTrip(o => o.Customer.Name.Contains("a") || o.Customer.IsVip);
        AssertPredicateRoundTrip(o => o.Items.Any(i => i.Price > 50m && i.Category == "Tech"));
        AssertPredicateRoundTrip(o => o.Discount > 5m);
        AssertPredicateRoundTrip(o => o.Notes != null && o.Notes.ContainsIgnoreCase("GIFT"));
        AssertPredicateRoundTrip(o => o.Items.Count > 1 && o.Items.Sum(i => i.Price * i.Quantity) > 100m);
        AssertPredicateRoundTrip(o => o.CreatedAt > new DateTime(2026, 1, 1) && o.DeliveredAt == null);
    }

    [Test]
    public void Selector_round_trips_with_result_type()
    {
        var serializer = Serializer();

        var model = serializer.ToModel<Order, string>(o => o.Customer.Name.ToUpper());
        var json = serializer.ToJson(model);
        var rebuilt = serializer.ModelFromJson<Order>(json).ToExpression<string>(serializer);

        Assert.That(rebuilt.Compile()(TestData.Orders[0]), Is.EqualTo("ALICE"));
    }

    [Test]
    public void Selector_to_object_result_gets_boxed()
    {
        var serializer = Serializer();

        var model = serializer.ToModel<Order, decimal>(o => o.Total);
        var boxed = serializer.ToExpression<Order, object>(serializer.ModelFromJson<Order>(serializer.ToJson(model)));

        Assert.That(boxed.Compile()(TestData.Orders[0]), Is.EqualTo(250.50m));
    }

    [Test]
    public void Model_static_helpers_work_end_to_end()
    {
        var model = ExpressionModel<Order>.From(o => o.Priority >= 2);
        var json = model.ToJson();
        var rebuilt = ExpressionModel<Order>.FromJson(json).ToPredicate();

        Assert.That(TestData.Orders.Count(rebuilt.Compile()), Is.EqualTo(4));
    }

    [Test]
    public void Untyped_model_with_extra_parameters_keeps_their_types()
    {
        Expression<Func<Order, decimal, bool>> lambda = (o, min) => o.Total >= min;
        var serializer = Serializer();

        var model = serializer.ToModel(lambda, typeof(Order));
        var json = serializer.ToJson(model);

        // Extra parameters cannot be inferred from the anchor and must carry their type.
        Assert.That(json, Does.Contain("decimal"));

        var rebuilt = serializer.ToLambda(
            System.Text.Json.JsonSerializer.Deserialize<ExpressionModel>(json, serializer.JsonOptions)!,
            typeof(Order));

        var compiled = (Func<Order, decimal, bool>)rebuilt.Compile();
        Assert.That(compiled(TestData.Orders[0], 100m), Is.True);
        Assert.That(compiled(TestData.Orders[1], 100m), Is.False);
    }

    [Test]
    public void Anonymous_projection_round_trips_through_model()
    {
        var serializer = Serializer();

        Expression<Func<Order, object>> projection = o => new { o.Id, Name = o.Customer.Name.ToUpper() };
        var model = serializer.ToModel(projection);
        var json = serializer.ToJson(model);

        var rebuilt = serializer.ModelFromJson<Order>(json).ToLambda(serializer);
        var project = (Func<Order, object>)rebuilt.Compile();

        Assert.That(project(TestData.Orders[0]).ToString(), Is.EqualTo("{ Id = 1, Name = ALICE }"));
    }

    [Test]
    public void Full_type_info_mode_is_available_for_models()
    {
        var serializer = Serializer();

        var model = serializer.ToModel<Order, bool>(o => o.Total > 100m, TypeInfoMode.Full);
        var json = serializer.ToJson(model);

        Assert.That(json, Does.Contain(typeof(Order).FullName!));
        Assert.That(json, Does.Contain("decimal"));

        var rebuilt = serializer.ModelFromJson<Order>(json).ToPredicate(serializer);
        Assert.That(rebuilt.Compile()(TestData.Orders[0]), Is.True);
    }

    [Test]
    public void Hand_written_minimal_filter()
    {
        const string json = """
        {
          "body": {
            "$type": "binary",
            "nodeType": "GreaterThan",
            "left": { "$type": "member", "member": { "name": "Total" }, "expression": { "$type": "parameter" } },
            "right": { "$type": "constant", "value": 100 }
          }
        }
        """;

        var predicate = Verify.Serializer().ModelFromJson<Order>(json).ToPredicate();
        var matches = TestData.Orders.Where(predicate.Compile()).Select(o => o.Id).ToList();

        Assert.That(matches, Is.EqualTo(new[] { 1, 3, 5, 6 }));
    }

    [Test]
    public void Hand_written_filter_with_camel_case_member_and_enum_string()
    {
        const string json = """
        {
          "body": {
            "$type": "binary",
            "nodeType": "AndAlso",
            "left": {
              "$type": "binary",
              "nodeType": "Equal",
              "left": { "$type": "member", "member": { "name": "status" }, "expression": { "$type": "parameter" } },
              "right": { "$type": "constant", "value": "Paid" }
            },
            "right": {
              "$type": "binary",
              "nodeType": "GreaterThanOrEqual",
              "left": { "$type": "member", "member": { "name": "total" }, "expression": { "$type": "parameter" } },
              "right": { "$type": "constant", "value": 250 }
            }
          }
        }
        """;

        var predicate = Verify.Serializer().ModelFromJson<Order>(json).ToPredicate();
        var matches = TestData.Orders.Where(predicate.Compile()).Select(o => o.Id).ToList();

        Assert.That(matches, Is.EqualTo(new[] { 1, 6 }));
    }

    [Test]
    public void Hand_written_filter_with_extension_method_inference()
    {
        // items.Any(i => i.price > 50) — no declaring types, no signatures, camelCase names:
        // the binder resolves Enumerable.Any<OrderItem> by unification and infers i : OrderItem.
        const string json = """
        {
          "body": {
            "$type": "call",
            "method": { "name": "any" },
            "object": { "$type": "member", "member": { "name": "items" }, "expression": { "$type": "parameter" } },
            "arguments": [
              {
                "$type": "lambda",
                "parameters": [ { "name": "i" } ],
                "body": {
                  "$type": "binary",
                  "nodeType": "GreaterThan",
                  "left": { "$type": "member", "member": { "name": "price" }, "expression": { "$type": "parameter", "name": "i" } },
                  "right": { "$type": "constant", "value": 50 }
                }
              }
            ]
          }
        }
        """;

        var predicate = Verify.Serializer().ModelFromJson<Order>(json).ToPredicate();
        var matches = TestData.Orders.Where(predicate.Compile()).Select(o => o.Id).ToList();

        Assert.That(matches, Is.EqualTo(new[] { 1, 3, 4, 6 }));
    }

    [Test]
    public void Hand_written_filter_with_string_method()
    {
        const string json = """
        {
          "body": {
            "$type": "call",
            "method": { "name": "contains" },
            "object": {
              "$type": "member",
              "member": { "name": "name" },
              "expression": { "$type": "member", "member": { "name": "customer" }, "expression": { "$type": "parameter" } }
            },
            "arguments": [ { "$type": "constant", "value": "li" } ]
          }
        }
        """;

        var predicate = Verify.Serializer().ModelFromJson<Order>(json).ToPredicate();
        var matches = TestData.Orders.Where(predicate.Compile()).Select(o => o.Id).ToList();

        Assert.That(matches, Is.EqualTo(new[] { 1, 4 }));
    }

    [Test]
    public void Hand_written_nullable_comparison()
    {
        const string json = """
        {
          "body": {
            "$type": "binary",
            "nodeType": "GreaterThan",
            "left": { "$type": "member", "member": { "name": "discount" }, "expression": { "$type": "parameter" } },
            "right": { "$type": "constant", "value": 15 }
          }
        }
        """;

        var predicate = Verify.Serializer().ModelFromJson<Order>(json).ToPredicate();
        var matches = TestData.Orders.Where(predicate.Compile()).Select(o => o.Id).ToList();

        Assert.That(matches, Is.EqualTo(new[] { 3, 5 }));
    }
}
