using System.Linq.Expressions;
using eQuantic.Linq.Expressions.Tests.Support;
using eQuantic.Linq.Expressions.Tests.TestModel;
using static eQuantic.Linq.Expressions.Tests.Support.Verify;

namespace eQuantic.Linq.Expressions.Tests;

[TestFixture]
public class MemberAndCallTests
{
    [Test]
    public void Property_chains()
    {
        Executes((Expression<Func<Order, string>>)(o => o.Customer.Name), Args(TestData.Orders[0]));
        Executes((Expression<Func<Order, string>>)(o => o.Customer.Address!.City), Args(TestData.Orders[0]), Args(TestData.Orders[4]));
        Executes((Expression<Func<Order, int>>)(o => o.Customer.Name.Length), Args(TestData.Orders[1]));
    }

    [Test]
    public void Field_access()
    {
        Executes((Expression<Func<Order, int>>)(o => o.Priority), Args(TestData.Orders[2]));
        Executes((Expression<Func<Order, bool>>)(o => o.Priority > 1), Args(TestData.Orders[2]), Args(TestData.Orders[1]));
    }

    [Test]
    public void Static_members_preserved_when_partial_evaluation_is_off()
    {
        var serializer = Serializer(o => o.EnablePartialEvaluation = false);

        ExecutesWith(serializer, (Expression<Func<double>>)(() => Math.PI * 2));
        ExecutesWith(serializer, (Expression<Func<string>>)(() => string.Empty));
        ExecutesWith(serializer, (Expression<Func<string>>)(() => StaticHelpers.Marker));
        ExecutesWith(serializer, (Expression<Func<decimal>>)(() => decimal.MaxValue));
    }

    [Test]
    public void String_methods()
    {
        var order = TestData.Orders[0];
        Executes((Expression<Func<Order, string>>)(o => o.Customer.Name.ToUpper()), Args(order));
        Executes((Expression<Func<Order, bool>>)(o => o.Customer.Name.Contains("li")), Args(order), Args(TestData.Orders[1]));
        Executes((Expression<Func<string, bool>>)(s => s.StartsWith("al")), Args("alpha"), Args("beta"));
        Executes((Expression<Func<string, string>>)(s => s.Substring(1, 3)), Args("abcdef"));
        Executes((Expression<Func<string, string>>)(s => s.Replace('a', 'o')), Args("banana"));
        Executes((Expression<Func<string, string[]>>)(s => s.Split(',')), Args("a,b,c"));
        Executes((Expression<Func<string, string, int>>)((a, b) => string.Compare(a, b, StringComparison.Ordinal)), Args("a", "b"));
    }

    [Test]
    public void Static_method_calls()
    {
        Executes((Expression<Func<int, int, int>>)((x, y) => Math.Max(x, y)), Args(3, 9));
        Executes((Expression<Func<double, double>>)(x => Math.Round(x, 2)), Args(3.14159));
        Executes((Expression<Func<int, string>>)(x => string.Format("id={0}|twice={1}", x, StaticHelpers.Twice(x))), Args(21));
        Executes((Expression<Func<string, int>>)(s => int.Parse(s)), Args("123"));
    }

    [Test]
    public void Generic_method_calls()
    {
        Executes((Expression<Func<int, int>>)(x => StaticHelpers.Echo(x)), Args(5));
        Executes((Expression<Func<string, string>>)(s => StaticHelpers.Echo(s)), Args("echo"));
        Executes((Expression<Func<Order, Order>>)(o => StaticHelpers.Echo(o)), Args(TestData.Orders[0]));
        Executes(
            (Expression<Func<Calculator, bool, string>>)((c, f) => c.Pick(f, "first", "second")),
            Args(new Calculator(), true),
            Args(new Calculator(), false));
    }

    [Test]
    public void Extension_method_calls()
    {
        Executes(
            (Expression<Func<Order, bool>>)(o => o.Notes != null && o.Notes.ContainsIgnoreCase("GIFT")),
            Args(TestData.Orders[3]),
            Args(TestData.Orders[0]),
            Args(TestData.Orders[1]));
    }

    [Test]
    public void Params_array_method_call()
    {
        Executes((Expression<Func<Calculator, int>>)(c => c.Sum(1, 2, 3)), Args(new Calculator { Seed = 10 }));
    }

    [Test]
    public void Indexers()
    {
        Executes((Expression<Func<List<int>, int>>)(l => l[1]), Args(new List<int> { 9, 8, 7 }));
        Executes(
            (Expression<Func<Dictionary<string, int>, int>>)(d => d["key"]),
            Args(new Dictionary<string, int> { ["key"] = 42 }));
        Executes((Expression<Func<Calculator, int>>)(c => c[2, 3]), Args(new Calculator { Seed = 100 }));
    }

    [Test]
    public void Index_expression_via_factory()
    {
        // IndexExpression (ExpressionType.Index) is only produced through the factory API.
        var calculator = Expression.Parameter(typeof(Calculator), "c");
        var indexer = Expression.MakeIndex(
            calculator,
            typeof(Calculator).GetProperty("Item"),
            [Expression.Constant(4), Expression.Constant(5)]);

        Executes(Expression.Lambda<Func<Calculator, int>>(indexer, calculator), Args(new Calculator { Seed = 1 }));

        var array = Expression.Parameter(typeof(int[]), "xs");
        var access = Expression.ArrayAccess(array, Expression.Constant(2));
        Executes(Expression.Lambda<Func<int[], int>>(access, array), Args(new object[] { new[] { 5, 6, 7 } }));
    }

    [Test]
    public void Multidimensional_array_access()
    {
        var matrix = new int[2, 3];
        matrix[1, 2] = 99;
        Executes((Expression<Func<int[,], int>>)(m => m[1, 2]), Args(new object[] { matrix }));
        Executes((Expression<Func<int[,], int>>)(m => m.GetLength(1)), Args(new object[] { matrix }));
    }

    [Test]
    public void Interface_typed_calls()
    {
        Executes(
            (Expression<Func<IEnumerable<int>, int>>)(source => source.Count()),
            Args(new object[] { new List<int> { 1, 2, 3 } }));

        Executes(
            (Expression<Func<IComparable<int>, int, int>>)((c, other) => c.CompareTo(other)),
            Args(5, 9));
    }

    [Test]
    public void ByRef_parameter_with_custom_delegate()
    {
        var value = Expression.Parameter(typeof(int).MakeByRefType(), "value");
        var body = Expression.Block(
            Expression.AddAssign(value, Expression.Constant(10)),
            value);

        var lambda = Expression.Lambda<RefIntOp>(body, value);
        var rebuilt = (Expression<RefIntOp>)Verify.RoundTrip(lambda);

        var original = lambda.Compile();
        var recovered = rebuilt.Compile();

        var a = 5;
        var b = 5;
        var resultA = original(ref a);
        var resultB = recovered(ref b);

        Assert.That(resultB, Is.EqualTo(resultA));
        Assert.That(b, Is.EqualTo(a));
        Assert.That(b, Is.EqualTo(15));
    }

    [Test]
    public void Nested_lambda_invocation()
    {
        var x = Expression.Parameter(typeof(int), "x");
        var y = Expression.Parameter(typeof(int), "y");
        var a = Expression.Parameter(typeof(int), "a");
        var b = Expression.Parameter(typeof(int), "b");
        var inner = Expression.Lambda<Func<int, int, int>>(Expression.Add(a, b), a, b);
        var body = Expression.Invoke(inner, x, y);

        Executes(Expression.Lambda<Func<int, int, int>>(body, x, y), Args(3, 4));

        // Nested lambda that captures the OUTER lambda's parameter.
        var outer = Expression.Parameter(typeof(int), "x");
        var innerParam = Expression.Parameter(typeof(int), "w");
        var closureLike = Expression.Lambda<Func<int, int>>(
            Expression.Invoke(
                Expression.Lambda<Func<int, int>>(Expression.Add(outer, innerParam), innerParam),
                Expression.Constant(100)),
            outer);
        Executes(closureLike, Args(7));
    }

    [Test]
    public void Quoted_nested_lambda()
    {
        // Expression.Quote — the mechanism behind IQueryable operators receiving Expression<> arguments.
        var inner = (Expression<Func<int, bool>>)(v => v > 2);
        var quote = Expression.Quote(inner);
        var lambda = Expression.Lambda<Func<Expression<Func<int, bool>>>>(quote);

        var rebuilt = Verify.RoundTrip(lambda);
        var recoveredInner = rebuilt.Compile()();

        Assert.That(recoveredInner.Compile()(3), Is.True);
        Assert.That(recoveredInner.Compile()(1), Is.False);
    }
}
