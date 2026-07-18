using System.Linq.Expressions;
using eQuantic.Linq.Expressions.Tests.Support;
using eQuantic.Linq.Expressions.Tests.TestModel;

namespace eQuantic.Linq.Expressions.Tests;

/// <summary>
/// Payloads loaded from .json files on disk — the exact shape of an external client (front-end,
/// another service) sending expressions over the wire, including the lean inferred format.
/// </summary>
[TestFixture]
public class PayloadFileTests
{
    private static string Load(string fileName)
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Payloads", fileName);
        Assert.That(File.Exists(path), Is.True, $"payload file not found: {path}");
        return File.ReadAllText(path);
    }

    [Test]
    public void Basic_filter_from_file()
    {
        var predicate = Verify.Serializer()
            .ModelFromJson<Order>(Load("order-filter-basic.json"))
            .ToPredicate();

        var matches = TestData.Orders.Where(predicate.Compile()).Select(o => o.Id).ToList();

        Assert.That(matches, Is.EqualTo(new[] { 1, 3, 6 }));
    }

    [Test]
    public void Nested_any_filter_from_file()
    {
        var predicate = Verify.Serializer()
            .ModelFromJson<Order>(Load("order-filter-nested-any.json"))
            .ToPredicate();

        var matches = TestData.Orders.Where(predicate.Compile()).Select(o => o.Id).ToList();

        Assert.That(matches, Is.EqualTo(new[] { 1, 3 }));
    }

    [Test]
    public void Selector_from_file()
    {
        var serializer = Verify.Serializer();
        var selector = serializer
            .ModelFromJson<Order>(Load("order-selector-name.json"))
            .ToExpression<string>(serializer);

        Assert.That(selector.Compile()(TestData.Orders[1]), Is.EqualTo("BRUNO"));
    }

    [Test]
    public void Full_query_pipeline_from_file()
    {
        // A whole IQueryable pipeline — root included — written by hand with zero type annotations.
        var serializer = Verify.Serializer();
        var lambda = (LambdaExpression)serializer.FromJson(Load("numbers-pipeline.json"));

        var result = lambda.Compile().DynamicInvoke();

        Assert.That(result, Is.EqualTo(TestData.Numbers.Where(n => n > 3).Sum()));
        Assert.That(result, Is.EqualTo(41));
    }

    [Test]
    public void Full_fidelity_format_from_file()
    {
        var predicate = Verify.Serializer().FromJson<Func<Order, bool>>(Load("order-filter-full-format.json"));

        var matches = TestData.Orders.Where(predicate.Compile()).Select(o => o.Id).ToList();

        Assert.That(matches, Is.EqualTo(new[] { 1, 3, 5, 6 }));
    }

    [Test]
    public void Rebuilt_file_payload_can_be_used_in_a_queryable_pipeline()
    {
        var predicate = Verify.Serializer()
            .ModelFromJson<Order>(Load("order-filter-basic.json"))
            .ToPredicate();

        var total = TestData.OrdersQuery.Where(predicate).Sum(o => o.Total);

        Assert.That(total, Is.EqualTo(250.50m + 1899.00m + 250.50m));
    }
}
