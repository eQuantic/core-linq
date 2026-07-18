using System.Linq.Expressions;
using eQuantic.Linq.Expressions.Tests.Support;
using static eQuantic.Linq.Expressions.Tests.Support.Verify;

namespace eQuantic.Linq.Expressions.Tests;

/// <summary>
/// Statement trees (blocks, loops, switch, try/catch, goto) can only be built through the factory API,
/// but they are full citizens of System.Linq.Expressions — and of this serializer.
/// </summary>
[TestFixture]
public class StatementTreeTests
{
    [Test]
    public void Factorial_with_block_loop_and_labels()
    {
        var n = Expression.Parameter(typeof(int), "n");
        var result = Expression.Variable(typeof(int), "result");
        var done = Expression.Label(typeof(int), "done");

        var body = Expression.Block(
            [result],
            Expression.Assign(result, Expression.Constant(1)),
            Expression.Loop(
                Expression.IfThenElse(
                    Expression.GreaterThan(n, Expression.Constant(1)),
                    Expression.Block(
                        Expression.MultiplyAssign(result, n),
                        Expression.PostDecrementAssign(n)),
                    Expression.Break(done, result)),
                done));

        var factorial = Expression.Lambda<Func<int, int>>(body, n);

        Executes(factorial, Args(0), Args(1), Args(5), Args(10));
    }

    [Test]
    public void Switch_with_multiple_test_values_and_default()
    {
        var value = Expression.Parameter(typeof(int), "value");

        var body = Expression.Switch(
            value,
            Expression.Constant("other"),
            Expression.SwitchCase(Expression.Constant("small"), Expression.Constant(1), Expression.Constant(2), Expression.Constant(3)),
            Expression.SwitchCase(Expression.Constant("ten"), Expression.Constant(10)));

        Executes(Expression.Lambda<Func<int, string>>(body, value), Args(2), Args(10), Args(99));
    }

    [Test]
    public void Switch_with_custom_comparison_method()
    {
        var value = Expression.Parameter(typeof(string), "value");

        var comparison = typeof(string).GetMethod(nameof(string.Equals), [typeof(string), typeof(string)])!;
        var body = Expression.Switch(
            typeof(string),
            value,
            Expression.Constant("none"),
            comparison,
            Expression.SwitchCase(Expression.Constant("greeting"), Expression.Constant("hello")),
            Expression.SwitchCase(Expression.Constant("farewell"), Expression.Constant("bye")));

        Executes(Expression.Lambda<Func<string, string>>(body, value), Args("hello"), Args("bye"), Args("hm"));
    }

    [Test]
    public void Try_catch_with_filter_and_finally()
    {
        var input = Expression.Parameter(typeof(int), "input");
        var log = Expression.Variable(typeof(string), "log");

        var tryBody = Expression.Condition(
            Expression.LessThan(input, Expression.Constant(0)),
            Expression.Throw(
                Expression.New(
                    typeof(ArgumentException).GetConstructor([typeof(string)])!,
                    Expression.Constant("negative value")),
                typeof(string)),
            Expression.Constant("ok"));

        var exception = Expression.Parameter(typeof(ArgumentException), "ex");
        var filtered = Expression.Catch(
            exception,
            Expression.Constant("caught-negative"),
            Expression.Call(
                Expression.Property(exception, nameof(Exception.Message)),
                nameof(string.Contains),
                null,
                Expression.Constant("negative")));

        var fallback = Expression.Catch(typeof(Exception), Expression.Constant("caught-other"));

        var tryExpression = Expression.TryCatchFinally(
            tryBody,
            Expression.Assign(log, Expression.Constant("finally-ran")),
            filtered,
            fallback);

        var body = Expression.Block(
            [log],
            Expression.Assign(log, Expression.Constant(string.Empty)),
            Expression.Call(
                typeof(string).GetMethod(nameof(string.Concat), [typeof(string), typeof(string), typeof(string)])!,
                tryExpression,
                Expression.Constant("|"),
                log));

        Executes(Expression.Lambda<Func<int, string>>(body, input), Args(5), Args(-5));
    }

    [Test]
    public void Rethrow_inside_catch_bubbles_to_outer_catch()
    {
        var inner = Expression.TryCatch(
            Expression.Block(
                Expression.Throw(
                    Expression.New(
                        typeof(InvalidOperationException).GetConstructor([typeof(string)])!,
                        Expression.Constant("boom"))),
                Expression.Constant("unreachable")),
            Expression.Catch(typeof(InvalidOperationException), Expression.Block(
                Expression.Rethrow(),
                Expression.Constant("unreachable-too"))));

        var exception = Expression.Parameter(typeof(InvalidOperationException), "ex");
        var outer = Expression.TryCatch(
            inner,
            Expression.Catch(exception, Expression.Property(exception, nameof(Exception.Message))));

        Executes(Expression.Lambda<Func<string>>(outer));
    }

    [Test]
    public void Throw_as_conditional_branch()
    {
        var x = Expression.Parameter(typeof(int), "x");
        var body = Expression.Condition(
            Expression.LessThanOrEqual(x, Expression.Constant(0)),
            Expression.Throw(
                Expression.New(
                    typeof(ArgumentOutOfRangeException).GetConstructor([typeof(string)])!,
                    Expression.Constant("x")),
                typeof(int)),
            Expression.Multiply(x, Expression.Constant(2)));

        Executes(Expression.Lambda<Func<int, int>>(body, x), Args(21), Args(0));
    }

    [Test]
    public void Assignments_and_compound_assignments()
    {
        var seed = Expression.Parameter(typeof(int), "seed");
        var v = Expression.Variable(typeof(int), "v");

        var body = Expression.Block(
            [v],
            Expression.Assign(v, seed),
            Expression.AddAssign(v, Expression.Constant(10)),
            Expression.MultiplyAssign(v, Expression.Constant(3)),
            Expression.SubtractAssign(v, Expression.Constant(4)),
            Expression.DivideAssign(v, Expression.Constant(2)),
            Expression.ModuloAssign(v, Expression.Constant(100)),
            Expression.LeftShiftAssign(v, Expression.Constant(1)),
            Expression.RightShiftAssign(v, Expression.Constant(1)),
            Expression.AndAssign(v, Expression.Constant(0xFFFF)),
            Expression.OrAssign(v, Expression.Constant(0b1)),
            Expression.ExclusiveOrAssign(v, Expression.Constant(0b10)),
            Expression.PreIncrementAssign(v),
            Expression.PostIncrementAssign(v),
            Expression.PreDecrementAssign(v),
            v);

        Executes(Expression.Lambda<Func<int, int>>(body, seed), Args(7), Args(-19));
    }

    [Test]
    public void Goto_skips_statements()
    {
        var v = Expression.Variable(typeof(int), "v");
        var skip = Expression.Label("skip");

        var body = Expression.Block(
            [v],
            Expression.Assign(v, Expression.Constant(1)),
            Expression.Goto(skip),
            Expression.Assign(v, Expression.Constant(999)),
            Expression.Label(skip),
            v);

        Executes(Expression.Lambda<Func<int>>(body));
    }

    [Test]
    public void Return_style_goto_with_value()
    {
        var x = Expression.Parameter(typeof(int), "x");
        var end = Expression.Label(typeof(string), "end");

        var body = Expression.Block(
            Expression.IfThen(
                Expression.GreaterThan(x, Expression.Constant(10)),
                Expression.Return(end, Expression.Constant("big"))),
            Expression.Label(end, Expression.Constant("small")));

        Executes(Expression.Lambda<Func<int, string>>(body, x), Args(50), Args(3));
    }

    [Test]
    public void Nested_blocks_with_shadowed_variable_names()
    {
        var outer = Expression.Variable(typeof(int), "v");
        var innerVariable = Expression.Variable(typeof(int), "v");

        var body = Expression.Block(
            [outer],
            Expression.Assign(outer, Expression.Constant(1)),
            Expression.Block(
                [innerVariable],
                Expression.Assign(innerVariable, Expression.Constant(100)),
                Expression.AddAssign(outer, innerVariable)),
            outer);

        Executes(Expression.Lambda<Func<int>>(body));
    }

    [Test]
    public void Void_lambda_with_side_effects()
    {
        var list = Expression.Parameter(typeof(List<int>), "list");
        var lambda = Expression.Lambda<Action<List<int>>>(
            Expression.Block(
                Expression.Call(list, nameof(List<int>.Add), null, Expression.Constant(7)),
                Expression.Call(list, nameof(List<int>.Add), null, Expression.Constant(8))),
            list);

        var rebuilt = (Expression<Action<List<int>>>)Verify.RoundTrip(lambda);

        var first = new List<int>();
        var second = new List<int>();
        lambda.Compile()(first);
        rebuilt.Compile()(second);

        Assert.That(second, Is.EqualTo(first));
        Assert.That(second, Is.EqualTo(new List<int> { 7, 8 }));
    }

    [Test]
    public void Runtime_variables_expression()
    {
        var x = Expression.Variable(typeof(int), "x");
        var body = Expression.Block(
            [x],
            Expression.Assign(x, Expression.Constant(42)),
            Expression.RuntimeVariables(x));

        var lambda = Expression.Lambda<Func<System.Runtime.CompilerServices.IRuntimeVariables>>(body);
        var rebuilt = (Expression<Func<System.Runtime.CompilerServices.IRuntimeVariables>>)Verify.RoundTrip(lambda);

        var original = lambda.Compile()();
        var recovered = rebuilt.Compile()();

        Assert.That(recovered.Count, Is.EqualTo(original.Count));
        Assert.That(recovered[0], Is.EqualTo(original[0]));
    }

    [Test]
    public void Debug_info_expressions()
    {
        var body = Expression.Block(
            Expression.DebugInfo(Expression.SymbolDocument("query.linq"), 1, 1, 2, 20),
            Expression.Constant(42));

        Executes(Expression.Lambda<Func<int>>(body));

        var cleared = Expression.Block(
            Expression.DebugInfo(Expression.SymbolDocument("query.linq"), 3, 1, 3, 5),
            Expression.ClearDebugInfo(Expression.SymbolDocument("query.linq")),
            Expression.Constant(1));

        Executes(Expression.Lambda<Func<int>>(cleared));
    }

    [Test]
    public void Try_fault_round_trips_structurally()
    {
        var tryFault = Expression.TryFault(
            Expression.Constant(1),
            Expression.Constant(0));

        // Fault blocks are exotic (unreachable via C#); prove structural fidelity.
        Verify.RoundTrip(Expression.Lambda<Func<int>>(tryFault));
    }
}
