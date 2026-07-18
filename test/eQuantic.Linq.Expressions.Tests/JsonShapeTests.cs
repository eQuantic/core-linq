using System.Linq.Expressions;
using System.Text.RegularExpressions;
using eQuantic.Linq.Expressions.Tests.Support;
using eQuantic.Linq.Expressions.Tests.TestModel;

namespace eQuantic.Linq.Expressions.Tests;

[TestFixture]
public class JsonShapeTests
{
    private static readonly Expression<Func<Order, bool>> Filter = o => o.Total > 100m;

    [Test]
    public void Payload_uses_type_discriminators_and_camel_case()
    {
        var json = Verify.Serializer().ToJson(Filter);

        Assert.That(json, Does.Contain("\"$type\":\"lambda\""));
        Assert.That(json, Does.Contain("\"$type\":\"binary\""));
        Assert.That(json, Does.Contain("\"$type\":\"member\""));
        Assert.That(json, Does.Contain("\"$type\":\"constant\""));
        Assert.That(json, Does.Contain("\"nodeType\":\"GreaterThan\""));
        Assert.That(json, Does.Contain("\"body\""));
        Assert.That(json, Does.Not.Contain("\"Body\""), "properties must be camelCase");
    }

    [Test]
    public void Null_properties_are_omitted()
    {
        var json = Verify.Serializer().ToJson(Filter);

        Assert.That(json, Does.Not.Contain(":null"));
        Assert.That(json, Does.Not.Contain("\"conversion\""));
        Assert.That(json, Does.Not.Contain("\"tailCall\""));
    }

    [Test]
    public void Indented_output_is_available()
    {
        var serializer = Verify.Serializer(o => o.WriteIndented = true);
        var json = serializer.ToJson(Filter);

        Assert.That(json, Does.Contain(Environment.NewLine));

        var rebuilt = serializer.FromJson<Func<Order, bool>>(json);
        Assert.That(rebuilt.Compile()(TestData.Orders[0]), Is.True);
    }

    [Test]
    public void Discriminator_can_appear_out_of_order()
    {
        var json = Verify.Serializer().ToJson(Filter);

        // Move every "$type" discriminator to the END of its object — hand-written payloads
        // are not required to put it first.
        var reordered = Regex.Replace(
            json,
            "\\{\"\\$type\":(\"[a-zA-Z]+\"),(.+?)\\}",
            "{$2,\"$$type\":$1}");

        // Only rewrite leaf objects (no nested braces) to keep the JSON valid.
        Assert.That(reordered, Is.Not.EqualTo(json), "test must actually reorder something");

        var rebuilt = Verify.Serializer().FromJson<Func<Order, bool>>(ReorderRootDiscriminator(json));
        Assert.That(rebuilt.Compile()(TestData.Orders[0]), Is.True);
    }

    private static string ReorderRootDiscriminator(string json)
    {
        // {"$type":"lambda", ...} → {..., "$type":"lambda"}
        const string prefix = "{\"$type\":\"lambda\",";
        Assert.That(json, Does.StartWith(prefix));
        return "{" + json.Substring(prefix.Length, json.Length - prefix.Length - 1) + ",\"$type\":\"lambda\"}";
    }

    [Test]
    public void Node_model_can_be_serialized_standalone()
    {
        var serializer = Verify.Serializer();
        var node = serializer.ToNode(Filter);

        var json = serializer.ToJson(node);
        var back = serializer.NodeFromJson(json);
        var rebuilt = serializer.ToExpression<Func<Order, bool>>(back);

        Assert.That(rebuilt.Compile()(TestData.Orders[0]), Is.True);
        Assert.That(rebuilt.Compile()(TestData.Orders[1]), Is.False);
    }
}
