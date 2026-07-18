using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using eQuantic.Linq.Expressions.Comparison;
using eQuantic.Linq.Expressions.Conversion;
using eQuantic.Linq.Expressions.Tests.TestModel;

namespace eQuantic.Linq.Expressions.Tests.Support;

/// <summary>
/// Uniform proof harness. For every expression it asserts:
/// 1. serialize → JSON → deserialize → serialize produces identical JSON (idempotence);
/// 2. the rebuilt tree is structurally equal to the (partially evaluated) original;
/// 3. compiling and executing both trees produces identical results for every argument set.
/// </summary>
internal static class Verify
{
    private static readonly JsonSerializerOptions ResultJson = new()
    {
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
    };

    public static ExpressionSerializer Serializer(Action<ExpressionSerializerOptions>? configure = null)
    {
        var options = new ExpressionSerializerOptions
        {
            QueryRootProvider = TestData.GetQueryable,
        };
        configure?.Invoke(options);
        return new ExpressionSerializer(options);
    }

    public static object?[] Args(params object?[] values) => values;

    public static LambdaExpression RoundTrip(
        LambdaExpression expression,
        bool checkStructure = true,
        ExpressionSerializer? serializer = null)
    {
        serializer ??= Serializer();

        var json = serializer.ToJson(expression);

        LambdaExpression rebuilt;
        try
        {
            rebuilt = (LambdaExpression)serializer.FromJson(json);
        }
        catch (Exception exception)
        {
            throw new Exception($"Rebuild failed for: {expression}\nJSON: {json}", exception);
        }

        var secondJson = serializer.ToJson(rebuilt);
        Assert.That(secondJson, Is.EqualTo(json), $"JSON round-trip is not idempotent for: {expression}");

        if (checkStructure)
        {
            var prepared = serializer.Options.EnablePartialEvaluation
                ? (LambdaExpression)PartialEvaluator.Eval(expression)
                : expression;

            Assert.That(
                ExpressionEqualityComparer.Instance.Equals(prepared, rebuilt),
                Is.True,
                $"Structural mismatch.\n original: {prepared}\n rebuilt:  {rebuilt}\n json: {json}");
        }

        return rebuilt;
    }

    public static Expression<TDelegate> RoundTrip<TDelegate>(
        Expression<TDelegate> expression,
        bool checkStructure = true,
        ExpressionSerializer? serializer = null) =>
        (Expression<TDelegate>)RoundTrip((LambdaExpression)expression, checkStructure, serializer);

    /// <summary>Round-trips and proves functional equivalence by executing both sides.</summary>
    public static void Executes(LambdaExpression expression, params object?[][] argSets) =>
        ExecutesCore(expression, checkStructure: true, serializer: null, argSets);

    /// <summary>Same as <see cref="Executes"/> but skips structural comparison (anonymous types rebuild to emitted types by design).</summary>
    public static void ExecutesShapeless(LambdaExpression expression, params object?[][] argSets) =>
        ExecutesCore(expression, checkStructure: false, serializer: null, argSets);

    public static void ExecutesWith(ExpressionSerializer serializer, LambdaExpression expression, params object?[][] argSets) =>
        ExecutesCore(expression, checkStructure: true, serializer, argSets);

    private static void ExecutesCore(
        LambdaExpression expression,
        bool checkStructure,
        ExpressionSerializer? serializer,
        object?[][] argSets)
    {
        var rebuilt = RoundTrip(expression, checkStructure, serializer);

        var original = expression.Compile();
        var recovered = rebuilt.Compile();

        if (argSets.Length == 0)
        {
            argSets = [[]];
        }

        foreach (var args in argSets)
        {
            var expected = Invoke(original, args);
            var actual = Invoke(recovered, args);

            Assert.That(
                AsJson(actual),
                Is.EqualTo(AsJson(expected)),
                $"Result mismatch for {expression} with args [{string.Join(", ", args.Select(a => a ?? "null"))}]");
        }
    }

    private static object? Invoke(Delegate target, object?[] args)
    {
        try
        {
            return target.DynamicInvoke(args);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            var inner = exception.InnerException;
            return $"threw {inner.GetType().Name}: {inner.Message}";
        }
    }

    public static string AsJson(object? value) =>
        value is null ? "null" : JsonSerializer.Serialize(value, value.GetType(), ResultJson);
}
