using System.Linq.Expressions;
using eQuantic.Linq.Expressions.Nodes;
using eQuantic.Linq.Expressions.Tests.Support;
using eQuantic.Linq.Expressions.Tests.TestModel;

namespace eQuantic.Linq.Expressions.Tests;

[TestFixture]
public class QueryRootTests
{
    private static Expression<Func<IQueryable<int>>> Pipeline => () => TestData.NumbersQuery.Where(n => n > 3);

    [Test]
    public void Query_root_is_serialized_as_query_root_node()
    {
        var node = Verify.Serializer().ToNode(Pipeline);

        QueryRootNode? root = null;
        void Walk(ExpressionNode current)
        {
            switch (current)
            {
                case QueryRootNode queryRoot:
                    root = queryRoot;
                    break;
                case LambdaNode lambda:
                    Walk(lambda.Body);
                    break;
                case MethodCallNode call:
                    if (call.Object is not null)
                    {
                        Walk(call.Object);
                    }

                    foreach (var argument in call.Arguments ?? [])
                    {
                        Walk(argument);
                    }

                    break;
                case UnaryNode unary when unary.Operand is not null:
                    Walk(unary.Operand);
                    break;
            }
        }

        Walk(node);

        Assert.That(root, Is.Not.Null, "pipeline must contain a query root node");
        Assert.That(root!.ElementType.Name, Is.EqualTo("int"));
    }

    [Test]
    public void Missing_provider_fails_with_actionable_message()
    {
        var json = Verify.Serializer().ToJson(Pipeline);

        var withoutProvider = new ExpressionSerializer();
        var exception = Assert.Throws<ExpressionSerializationException>(() => withoutProvider.FromJson(json));

        Assert.That(exception!.Message, Does.Contain("QueryRootProvider"));
    }

    [Test]
    public void Provider_returning_null_fails_with_actionable_message()
    {
        var json = Verify.Serializer().ToJson(Pipeline);

        var nullProvider = new ExpressionSerializer(new ExpressionSerializerOptions
        {
            QueryRootProvider = _ => null,
        });

        var exception = Assert.Throws<ExpressionSerializationException>(() => nullProvider.FromJson(json));
        Assert.That(exception!.Message, Does.Contain("null"));
    }

    [Test]
    public void Rebound_query_executes_against_provider_supplied_source()
    {
        var json = Verify.Serializer().ToJson(Pipeline);

        // The "server side" re-binds the root to its own data source.
        var serverData = new List<int> { 1, 10, 20 }.AsQueryable();
        var server = new ExpressionSerializer(new ExpressionSerializerOptions
        {
            QueryRootProvider = _ => serverData,
        });

        var rebuilt = (Expression<Func<IQueryable<int>>>)server.FromJson(json);
        var results = rebuilt.Compile()().ToList();

        Assert.That(results, Is.EqualTo(new[] { 10, 20 }));
    }
}
