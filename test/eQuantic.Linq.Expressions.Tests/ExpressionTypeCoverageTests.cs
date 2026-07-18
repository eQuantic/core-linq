using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using eQuantic.Linq.Expressions.Tests.Support;
using eQuantic.Linq.Expressions.Tests.TestModel;

namespace eQuantic.Linq.Expressions.Tests;

/// <summary>
/// The formal coverage proof: every single <see cref="ExpressionType"/> value must either round-trip
/// with full structural fidelity or be explicitly documented as impossible to serialize.
/// </summary>
[TestFixture]
public class ExpressionTypeCoverageTests
{
    private sealed record Entry(Func<LambdaExpression>? Build, string? UnsupportedReason = null, bool ViaReduction = false);

    private sealed class DoubledExtension : Expression
    {
        private readonly Expression _operand;

        public DoubledExtension(Expression operand) => _operand = operand;

        public override ExpressionType NodeType => ExpressionType.Extension;

        public override Type Type => _operand.Type;

        public override bool CanReduce => true;

        public override Expression Reduce() => Multiply(_operand, Constant(2));
    }

    private sealed class DummyBinder : CallSiteBinder
    {
        public override Expression Bind(object[] args, System.Collections.ObjectModel.ReadOnlyCollection<ParameterExpression> parameters, LabelTarget returnLabel) =>
            throw new NotSupportedException();
    }

    private static LambdaExpression IntBinary(Func<Expression, Expression, Expression> factory)
    {
        var x = Expression.Parameter(typeof(int), "x");
        var y = Expression.Parameter(typeof(int), "y");
        return Expression.Lambda<Func<int, int, int>>(factory(x, y), x, y);
    }

    private static LambdaExpression IntComparison(Func<Expression, Expression, Expression> factory)
    {
        var x = Expression.Parameter(typeof(int), "x");
        var y = Expression.Parameter(typeof(int), "y");
        return Expression.Lambda<Func<int, int, bool>>(factory(x, y), x, y);
    }

    private static LambdaExpression BoolBinary(Func<Expression, Expression, Expression> factory)
    {
        var x = Expression.Parameter(typeof(bool), "x");
        var y = Expression.Parameter(typeof(bool), "y");
        return Expression.Lambda<Func<bool, bool, bool>>(factory(x, y), x, y);
    }

    private static LambdaExpression IntUnary(Func<Expression, Expression> factory)
    {
        var x = Expression.Parameter(typeof(int), "x");
        return Expression.Lambda<Func<int, int>>(factory(x), x);
    }

    private static LambdaExpression CompoundAssign(Func<Expression, Expression, Expression> factory)
    {
        var seed = Expression.Parameter(typeof(int), "seed");
        var v = Expression.Variable(typeof(int), "v");
        return Expression.Lambda<Func<int, int>>(
            Expression.Block([v], Expression.Assign(v, seed), factory(v, Expression.Constant(3)), v),
            seed);
    }

    private static LambdaExpression IncrementAssign(Func<Expression, Expression> factory)
    {
        var seed = Expression.Parameter(typeof(int), "seed");
        var v = Expression.Variable(typeof(int), "v");
        return Expression.Lambda<Func<int, int>>(
            Expression.Block([v], Expression.Assign(v, seed), factory(v), v),
            seed);
    }

    private static readonly Dictionary<ExpressionType, Entry> Matrix = new()
    {
        [ExpressionType.Add] = new(() => IntBinary(Expression.Add)),
        [ExpressionType.AddChecked] = new(() => IntBinary(Expression.AddChecked)),
        [ExpressionType.And] = new(() => IntBinary(Expression.And)),
        [ExpressionType.AndAlso] = new(() => BoolBinary(Expression.AndAlso)),
        [ExpressionType.ArrayLength] = new(() =>
        {
            var a = Expression.Parameter(typeof(int[]), "a");
            return Expression.Lambda<Func<int[], int>>(Expression.ArrayLength(a), a);
        }),
        [ExpressionType.ArrayIndex] = new(() =>
        {
            var a = Expression.Parameter(typeof(int[]), "a");
            var i = Expression.Parameter(typeof(int), "i");
            return Expression.Lambda<Func<int[], int, int>>(Expression.ArrayIndex(a, i), a, i);
        }),
        [ExpressionType.Call] = new(() => IntBinary((x, y) => Expression.Call(typeof(Math), nameof(Math.Max), null, x, y))),
        [ExpressionType.Coalesce] = new(() =>
        {
            var p = Expression.Parameter(typeof(int?), "p");
            return Expression.Lambda<Func<int?, int>>(Expression.Coalesce(p, Expression.Constant(-1)), p);
        }),
        [ExpressionType.Conditional] = new(() =>
        {
            var x = Expression.Parameter(typeof(int), "x");
            return Expression.Lambda<Func<int, string>>(
                Expression.Condition(
                    Expression.GreaterThan(x, Expression.Constant(0)),
                    Expression.Constant("pos"),
                    Expression.Constant("neg")),
                x);
        }),
        [ExpressionType.Constant] = new(() => Expression.Lambda<Func<int>>(Expression.Constant(42))),
        [ExpressionType.Convert] = new(() =>
        {
            var x = Expression.Parameter(typeof(int), "x");
            return Expression.Lambda<Func<int, long>>(Expression.Convert(x, typeof(long)), x);
        }),
        [ExpressionType.ConvertChecked] = new(() =>
        {
            var x = Expression.Parameter(typeof(int), "x");
            return Expression.Lambda<Func<int, byte>>(Expression.ConvertChecked(x, typeof(byte)), x);
        }),
        [ExpressionType.Divide] = new(() => IntBinary(Expression.Divide)),
        [ExpressionType.Equal] = new(() => IntComparison(Expression.Equal)),
        [ExpressionType.ExclusiveOr] = new(() => IntBinary(Expression.ExclusiveOr)),
        [ExpressionType.GreaterThan] = new(() => IntComparison(Expression.GreaterThan)),
        [ExpressionType.GreaterThanOrEqual] = new(() => IntComparison(Expression.GreaterThanOrEqual)),
        [ExpressionType.Invoke] = new(() =>
        {
            var x = Expression.Parameter(typeof(int), "x");
            var a = Expression.Parameter(typeof(int), "a");
            var inner = Expression.Lambda<Func<int, int>>(Expression.Add(a, Expression.Constant(1)), a);
            return Expression.Lambda<Func<int, int>>(Expression.Invoke(inner, x), x);
        }),
        [ExpressionType.Lambda] = new(() =>
        {
            var a = Expression.Parameter(typeof(int), "a");
            var inner = Expression.Lambda<Func<int, int>>(Expression.Negate(a), a);
            return Expression.Lambda<Func<Func<int, int>>>(inner);
        }),
        [ExpressionType.LeftShift] = new(() => IntBinary(Expression.LeftShift)),
        [ExpressionType.LessThan] = new(() => IntComparison(Expression.LessThan)),
        [ExpressionType.LessThanOrEqual] = new(() => IntComparison(Expression.LessThanOrEqual)),
        [ExpressionType.ListInit] = new(() =>
        {
            var x = Expression.Parameter(typeof(int), "x");
            return (LambdaExpression)(Expression<Func<int, List<int>>>)(v => new List<int> { 1, v });
        }),
        [ExpressionType.MemberAccess] = new(() => (Expression<Func<Order, decimal>>)(o => o.Total)),
        [ExpressionType.MemberInit] = new(() => (Expression<Func<int, OrderItem>>)(v => new OrderItem { Id = v, Product = "p" })),
        [ExpressionType.Modulo] = new(() => IntBinary(Expression.Modulo)),
        [ExpressionType.Multiply] = new(() => IntBinary(Expression.Multiply)),
        [ExpressionType.MultiplyChecked] = new(() => IntBinary(Expression.MultiplyChecked)),
        [ExpressionType.Negate] = new(() => IntUnary(Expression.Negate)),
        [ExpressionType.UnaryPlus] = new(() => IntUnary(Expression.UnaryPlus)),
        [ExpressionType.NegateChecked] = new(() => IntUnary(Expression.NegateChecked)),
        [ExpressionType.New] = new(() => (Expression<Func<string, OrderItem>>)(p => new OrderItem(p, 1m, 2))),
        [ExpressionType.NewArrayInit] = new(() => (Expression<Func<int, int[]>>)(v => new[] { v, v + 1 })),
        [ExpressionType.NewArrayBounds] = new(() => (Expression<Func<int, int[]>>)(n => new int[n])),
        [ExpressionType.Not] = new(() =>
        {
            var b = Expression.Parameter(typeof(bool), "b");
            return Expression.Lambda<Func<bool, bool>>(Expression.Not(b), b);
        }),
        [ExpressionType.NotEqual] = new(() => IntComparison(Expression.NotEqual)),
        [ExpressionType.Or] = new(() => IntBinary(Expression.Or)),
        [ExpressionType.OrElse] = new(() => BoolBinary(Expression.OrElse)),
        [ExpressionType.Parameter] = new(() => (Expression<Func<int, int>>)(x => x)),
        [ExpressionType.Power] = new(() =>
        {
            var x = Expression.Parameter(typeof(double), "x");
            var y = Expression.Parameter(typeof(double), "y");
            return Expression.Lambda<Func<double, double, double>>(Expression.Power(x, y), x, y);
        }),
        [ExpressionType.Quote] = new(() =>
        {
            var inner = Expression.Lambda<Func<int>>(Expression.Constant(7));
            return Expression.Lambda<Func<Expression<Func<int>>>>(Expression.Quote(inner));
        }),
        [ExpressionType.RightShift] = new(() => IntBinary(Expression.RightShift)),
        [ExpressionType.Subtract] = new(() => IntBinary(Expression.Subtract)),
        [ExpressionType.SubtractChecked] = new(() => IntBinary(Expression.SubtractChecked)),
        [ExpressionType.TypeAs] = new(() =>
        {
            var o = Expression.Parameter(typeof(object), "o");
            return Expression.Lambda<Func<object, string?>>(Expression.TypeAs(o, typeof(string)), o);
        }),
        [ExpressionType.TypeIs] = new(() =>
        {
            var o = Expression.Parameter(typeof(object), "o");
            return Expression.Lambda<Func<object, bool>>(Expression.TypeIs(o, typeof(string)), o);
        }),
        [ExpressionType.Assign] = new(() => CompoundAssign(Expression.Assign)),
        [ExpressionType.Block] = new(() =>
        {
            var v = Expression.Variable(typeof(int), "v");
            return Expression.Lambda<Func<int>>(
                Expression.Block([v], Expression.Assign(v, Expression.Constant(1)), v));
        }),
        [ExpressionType.DebugInfo] = new(() => Expression.Lambda<Func<int>>(
            Expression.Block(
                Expression.DebugInfo(Expression.SymbolDocument("proof.linq"), 1, 1, 1, 10),
                Expression.Constant(1)))),
        [ExpressionType.Decrement] = new(() => IntUnary(Expression.Decrement)),
        [ExpressionType.Dynamic] = new(
            null,
            "Dynamic call sites carry runtime binders (CallSiteBinder) that have no portable representation."),
        [ExpressionType.Default] = new(() => Expression.Lambda<Func<int>>(Expression.Default(typeof(int)))),
        [ExpressionType.Extension] = new(() =>
        {
            var x = Expression.Parameter(typeof(int), "x");
            return Expression.Lambda<Func<int, int>>(new DoubledExtension(x), x);
        }, ViaReduction: true),
        [ExpressionType.Goto] = new(() =>
        {
            var v = Expression.Variable(typeof(int), "v");
            var skip = Expression.Label("skip");
            return Expression.Lambda<Func<int>>(Expression.Block(
                [v],
                Expression.Assign(v, Expression.Constant(1)),
                Expression.Goto(skip),
                Expression.Assign(v, Expression.Constant(2)),
                Expression.Label(skip),
                v));
        }),
        [ExpressionType.Increment] = new(() => IntUnary(Expression.Increment)),
        [ExpressionType.Index] = new(() =>
        {
            var c = Expression.Parameter(typeof(Calculator), "c");
            return Expression.Lambda<Func<Calculator, int>>(
                Expression.MakeIndex(c, typeof(Calculator).GetProperty("Item"), [Expression.Constant(1), Expression.Constant(2)]),
                c);
        }),
        [ExpressionType.Label] = new(() => Expression.Lambda<Func<int>>(
            Expression.Label(Expression.Label(typeof(int), "value"), Expression.Constant(9)))),
        [ExpressionType.RuntimeVariables] = new(() =>
        {
            var v = Expression.Variable(typeof(int), "v");
            return Expression.Lambda<Func<IRuntimeVariables>>(Expression.Block(
                [v],
                Expression.Assign(v, Expression.Constant(1)),
                Expression.RuntimeVariables(v)));
        }),
        [ExpressionType.Loop] = new(() =>
        {
            var v = Expression.Variable(typeof(int), "v");
            var brk = Expression.Label(typeof(int), "brk");
            var cont = Expression.Label("cont");
            return Expression.Lambda<Func<int>>(Expression.Block(
                [v],
                Expression.Assign(v, Expression.Constant(0)),
                Expression.Loop(
                    Expression.Block(
                        Expression.IfThen(
                            Expression.GreaterThanOrEqual(v, Expression.Constant(3)),
                            Expression.Break(brk, v)),
                        Expression.PreIncrementAssign(v),
                        Expression.Continue(cont)),
                    brk,
                    cont)));
        }),
        [ExpressionType.Switch] = new(() =>
        {
            var x = Expression.Parameter(typeof(int), "x");
            return Expression.Lambda<Func<int, string>>(
                Expression.Switch(
                    x,
                    Expression.Constant("default"),
                    Expression.SwitchCase(Expression.Constant("one"), Expression.Constant(1))),
                x);
        }),
        [ExpressionType.Throw] = new(() =>
        {
            var x = Expression.Parameter(typeof(int), "x");
            return Expression.Lambda<Func<int, int>>(
                Expression.Condition(
                    Expression.LessThan(x, Expression.Constant(0)),
                    Expression.Throw(Expression.New(typeof(InvalidOperationException)), typeof(int)),
                    x),
                x);
        }),
        [ExpressionType.Try] = new(() => Expression.Lambda<Func<int>>(
            Expression.TryCatchFinally(
                Expression.Constant(1),
                Expression.Empty(),
                Expression.Catch(typeof(Exception), Expression.Constant(-1))))),
        [ExpressionType.Unbox] = new(() =>
        {
            var o = Expression.Parameter(typeof(object), "o");
            return Expression.Lambda<Func<object, int>>(Expression.Unbox(o, typeof(int)), o);
        }),
        [ExpressionType.AddAssign] = new(() => CompoundAssign(Expression.AddAssign)),
        [ExpressionType.AndAssign] = new(() => CompoundAssign(Expression.AndAssign)),
        [ExpressionType.DivideAssign] = new(() => CompoundAssign(Expression.DivideAssign)),
        [ExpressionType.ExclusiveOrAssign] = new(() => CompoundAssign(Expression.ExclusiveOrAssign)),
        [ExpressionType.LeftShiftAssign] = new(() => CompoundAssign(Expression.LeftShiftAssign)),
        [ExpressionType.ModuloAssign] = new(() => CompoundAssign(Expression.ModuloAssign)),
        [ExpressionType.MultiplyAssign] = new(() => CompoundAssign(Expression.MultiplyAssign)),
        [ExpressionType.OrAssign] = new(() => CompoundAssign(Expression.OrAssign)),
        [ExpressionType.PowerAssign] = new(() =>
        {
            var seed = Expression.Parameter(typeof(double), "seed");
            var v = Expression.Variable(typeof(double), "v");
            return Expression.Lambda<Func<double, double>>(
                Expression.Block([v], Expression.Assign(v, seed), Expression.PowerAssign(v, Expression.Constant(2.0)), v),
                seed);
        }),
        [ExpressionType.RightShiftAssign] = new(() => CompoundAssign(Expression.RightShiftAssign)),
        [ExpressionType.SubtractAssign] = new(() => CompoundAssign(Expression.SubtractAssign)),
        [ExpressionType.AddAssignChecked] = new(() => CompoundAssign(Expression.AddAssignChecked)),
        [ExpressionType.MultiplyAssignChecked] = new(() => CompoundAssign(Expression.MultiplyAssignChecked)),
        [ExpressionType.SubtractAssignChecked] = new(() => CompoundAssign(Expression.SubtractAssignChecked)),
        [ExpressionType.PreIncrementAssign] = new(() => IncrementAssign(Expression.PreIncrementAssign)),
        [ExpressionType.PreDecrementAssign] = new(() => IncrementAssign(Expression.PreDecrementAssign)),
        [ExpressionType.PostIncrementAssign] = new(() => IncrementAssign(Expression.PostIncrementAssign)),
        [ExpressionType.PostDecrementAssign] = new(() => IncrementAssign(Expression.PostDecrementAssign)),
        [ExpressionType.TypeEqual] = new(() =>
        {
            var o = Expression.Parameter(typeof(object), "o");
            return Expression.Lambda<Func<object, bool>>(Expression.TypeEqual(o, typeof(string)), o);
        }),
        [ExpressionType.OnesComplement] = new(() => IntUnary(Expression.OnesComplement)),
        [ExpressionType.IsTrue] = new(() =>
        {
            var b = Expression.Parameter(typeof(bool), "b");
            return Expression.Lambda<Func<bool, bool>>(Expression.IsTrue(b), b);
        }),
        [ExpressionType.IsFalse] = new(() =>
        {
            var b = Expression.Parameter(typeof(bool), "b");
            return Expression.Lambda<Func<bool, bool>>(Expression.IsFalse(b), b);
        }),
    };

    [Test]
    public void Every_expression_type_is_covered_by_the_matrix()
    {
        var missing = Enum.GetValues<ExpressionType>().Where(value => !Matrix.ContainsKey(value)).ToList();
        Assert.That(missing, Is.Empty, $"ExpressionType values missing from the coverage matrix: {string.Join(", ", missing)}");
    }

    [Test]
    public void Every_supported_expression_type_round_trips_with_full_fidelity()
    {
        var serializer = Verify.Serializer(o => o.EnablePartialEvaluation = false);
        var report = new List<string>();
        var failures = new List<string>();

        foreach (var pair in Matrix.OrderBy(p => p.Key.ToString(), StringComparer.Ordinal))
        {
            var (expressionType, entry) = (pair.Key, pair.Value);

            if (entry.Build is null)
            {
                report.Add($"{expressionType,-28} NOT SUPPORTED — {entry.UnsupportedReason}");
                continue;
            }

            try
            {
                var lambda = entry.Build();

                if (entry.ViaReduction)
                {
                    // Extension nodes serialize through Reduce(); compare against the reduced tree.
                    var json = serializer.ToJson(lambda);
                    var rebuilt = (LambdaExpression)serializer.FromJson(json);
                    Assert.That(serializer.ToJson(rebuilt), Is.EqualTo(json));
                    report.Add($"{expressionType,-28} SUPPORTED (via Reduce())");
                    continue;
                }

                Verify.RoundTrip(lambda, checkStructure: true, serializer);
                report.Add($"{expressionType,-28} SUPPORTED");
            }
            catch (Exception exception)
            {
                failures.Add($"{expressionType}: {exception.Message}");
                report.Add($"{expressionType,-28} FAILED");
            }
        }

        TestContext.Out.WriteLine("ExpressionType support matrix");
        TestContext.Out.WriteLine("=============================");
        foreach (var line in report)
        {
            TestContext.Out.WriteLine(line);
        }

        Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));

        var supported = Matrix.Count(p => p.Value.Build is not null);
        var unsupported = Matrix.Count(p => p.Value.Build is null);
        TestContext.Out.WriteLine($"Supported: {supported}/{Matrix.Count} — Unsupported by design: {unsupported} (Dynamic)");

        Assert.That(unsupported, Is.EqualTo(1), "only ExpressionType.Dynamic is expected to be unsupported");
    }

    [Test]
    public void Dynamic_expression_fails_with_actionable_message()
    {
        var dynamicExpression = Expression.Dynamic(new DummyBinder(), typeof(object), Expression.Constant(1));
        var lambda = Expression.Lambda<Func<object>>(dynamicExpression);

        var serializer = Verify.Serializer(o => o.EnablePartialEvaluation = false);
        var exception = Assert.Throws<ExpressionSerializationException>(() => serializer.ToJson(lambda));

        Assert.That(exception!.Message, Does.Contain("Dynamic"));
    }

    [Test]
    public void Non_reducible_extension_fails_with_actionable_message()
    {
        var serializer = Verify.Serializer(o => o.EnablePartialEvaluation = false);
        var lambda = Expression.Lambda<Func<int>>(new NonReducible());

        var exception = Assert.Throws<ExpressionSerializationException>(() => serializer.ToJson(lambda));
        Assert.That(exception!.Message, Does.Contain("not supported"));
    }

    private sealed class NonReducible : Expression
    {
        public override ExpressionType NodeType => ExpressionType.Extension;

        public override Type Type => typeof(int);

        public override bool CanReduce => false;
    }
}
