using System.Linq.Expressions;
using eQuantic.Linq.Expressions.Nodes;
using eQuantic.Linq.Expressions.Tests.Support;
using eQuantic.Linq.Expressions.Tests.TestModel;
using static eQuantic.Linq.Expressions.Tests.Support.Verify;

namespace eQuantic.Linq.Expressions.Tests;

/// <summary>
/// Proves that compiler-generated closures (captured locals, <c>this</c> references, static state)
/// are folded into portable constants before serialization — the pre-requisite for transporting
/// real-world C# lambdas across process boundaries.
/// </summary>
[TestFixture]
public class ClosureTests
{
    private readonly decimal _factor = 1.5m;

    private static ConstantNode? FindConstant(ExpressionNode node) => node switch
    {
        ConstantNode constant => constant,
        LambdaNode lambda => FindConstant(lambda.Body),
        BinaryNode binary => FindConstant(binary.Left) ?? FindConstant(binary.Right),
        MethodCallNode call => (call.Object is null ? null : FindConstant(call.Object))
                               ?? call.Arguments?.Select(FindConstant).FirstOrDefault(c => c is not null),
        UnaryNode unary => unary.Operand is null ? null : FindConstant(unary.Operand),
        MemberNode member => member.Expression is null ? null : FindConstant(member.Expression),
        _ => null,
    };

    [Test]
    public void Captured_local_variable_folds_to_constant()
    {
        var threshold = 100m;
        Expression<Func<Order, bool>> lambda = o => o.Total > threshold;

        var node = Serializer().ToNode(lambda);
        var constant = FindConstant(node);

        Assert.That(constant, Is.Not.Null, "captured local should fold into a constant");
        Assert.That(constant!.Value, Is.EqualTo(100m));

        Executes(lambda, Args(TestData.Orders[0]), Args(TestData.Orders[1]));
    }

    [Test]
    public void Captured_collection_folds_and_method_call_survives()
    {
        var ids = new List<int> { 1, 3, 5 };
        Expression<Func<Order, bool>> lambda = o => ids.Contains(o.Id);

        var node = (LambdaNode)Serializer().ToNode(lambda);
        Assert.That(node.Body, Is.InstanceOf<MethodCallNode>(), "Contains call must stay structural");
        var call = (MethodCallNode)node.Body;
        Assert.That(call.Object, Is.InstanceOf<ConstantNode>(), "captured list must fold into a constant");

        Executes(lambda, Args(TestData.Orders[0]), Args(TestData.Orders[1]), Args(TestData.Orders[2]));
    }

    [Test]
    public void Captured_array_span_contains_folds_and_serializes()
    {
        // In modern .NET, `array.Contains(value)` binds to MemoryExtensions.Contains(ReadOnlySpan<T>, T)
        // through an implicit array→ReadOnlySpan<T> conversion (op_Implicit). That conversion node is typed
        // ReadOnlySpan<int> — a ByRefLike ref struct that cannot be boxed to object. Nominating it for local
        // evaluation used to throw ExpressionSerializationException; instead the captured array operand must
        // fold to a constant while the span-conversion node stays structural.
        var ids = new[] { 1, 2, 3 };
        Expression<Func<Order, bool>> lambda = o => ids.Contains(o.Id);

        LambdaNode node = null!;
        Assert.That(() => node = (LambdaNode)Serializer().ToNode(lambda), Throws.Nothing,
            "Span-bound Contains must partial-evaluate without throwing");

        Assert.That(node.Body, Is.InstanceOf<MethodCallNode>(), "Contains call must stay structural");
        var conversion = ((MethodCallNode)node.Body).Arguments![0];
        Assert.That(conversion, Is.InstanceOf<MethodCallNode>(), "array→ReadOnlySpan conversion must stay structural");
        var folded = ((MethodCallNode)conversion).Arguments![0];
        Assert.That(folded, Is.InstanceOf<ConstantNode>(), "captured array must fold into a constant");
        Assert.That(((ConstantNode)folded).Value, Is.EqualTo(new[] { 1, 2, 3 }));

        Executes(lambda, Args(TestData.Orders[0]), Args(TestData.Orders[1]), Args(TestData.Orders[2]));
    }

    [Test]
    public void Inline_array_span_contains_serializes()
    {
        // Same ReadOnlySpan<int> span-conversion node as the captured case, but with an inline `new[] {…}`
        // operand: the NewArrayInit stays structural, so the conversion node is never nominated for
        // evaluation. It must still serialize, round-trip, and execute.
        Expression<Func<Order, bool>> lambda = o => new[] { 1, 2, 3 }.Contains(o.Id);

        Assert.That(() => Serializer().ToNode(lambda), Throws.Nothing);

        Executes(lambda, Args(TestData.Orders[0]), Args(TestData.Orders[1]), Args(TestData.Orders[2]));
    }

    [Test]
    public void Captured_this_member_folds_to_constant()
    {
        Expression<Func<Order, decimal>> lambda = o => o.Total * _factor;

        var node = (LambdaNode)Serializer().ToNode(lambda);
        var binary = (BinaryNode)node.Body;
        Assert.That(binary.Right, Is.InstanceOf<ConstantNode>(), "this._factor must fold into a constant");

        Executes(lambda, Args(TestData.Orders[0]));
    }

    [Test]
    public void Static_readonly_field_folds_to_constant()
    {
        Expression<Func<string>> lambda = () => StaticHelpers.Marker;

        var node = (LambdaNode)Serializer().ToNode(lambda);
        Assert.That(node.Body, Is.InstanceOf<ConstantNode>());
        Assert.That(((ConstantNode)node.Body).Value, Is.EqualTo("marker"));

        Executes(lambda);
    }

    [Test]
    public void Capture_inside_nested_lambda_folds()
    {
        var minimumPrice = 100m;
        Expression<Func<Order, bool>> lambda = o => o.Items.Any(i => i.Price > minimumPrice);

        Executes(lambda, Args(TestData.Orders[0]), Args(TestData.Orders[1]), Args(TestData.Orders[2]));
    }

    [Test]
    public void Captured_computed_expression_folds_to_final_value()
    {
        var baseValue = 10;
        var offset = 32;
        Expression<Func<int, int>> lambda = x => x + (baseValue + offset);

        var node = (LambdaNode)Serializer().ToNode(lambda);
        var constant = FindConstant(node.Body);
        Assert.That(constant?.Value, Is.EqualTo(42));

        Executes(lambda, Args(8));
    }

    [Test]
    public void Captured_array_element_folds()
    {
        var values = new[] { 10, 20, 30 };
        Expression<Func<int, int>> lambda = x => x + values[1];

        Executes(lambda, Args(5));
    }

    [Test]
    public void Captured_object_graph_folds_and_serializes_as_value()
    {
        var template = new OrderItem { Product = "Template", Price = 9.99m, Quantity = 1 };
        Expression<Func<Order, bool>> lambda = o => o.Total > template.Price;

        Executes(lambda, Args(TestData.Orders[0]), Args(TestData.Orders[1]));
    }

    [Test]
    public void Structure_preserving_nodes_are_not_folded()
    {
        // `new DateTime(...)` has only constant inputs, but object creation must stay structural.
        Expression<Func<Order, bool>> lambda = o => o.CreatedAt > new DateTime(2026, 1, 1);

        var node = (LambdaNode)Serializer().ToNode(lambda);
        var binary = (BinaryNode)node.Body;
        Assert.That(binary.Right, Is.InstanceOf<NewNode>(), "new T(...) must remain a structural node");

        Executes(lambda, Args(TestData.Orders[0]), Args(TestData.Orders[3]));
    }
}
