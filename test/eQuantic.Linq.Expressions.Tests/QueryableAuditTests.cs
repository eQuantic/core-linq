using System.Linq.Expressions;
using System.Reflection;
using eQuantic.Linq.Expressions.Tests.Support;
using eQuantic.Linq.Expressions.Tests.TestModel;
using static eQuantic.Linq.Expressions.Tests.Support.Verify;

namespace eQuantic.Linq.Expressions.Tests;

/// <summary>
/// Audit-driven completion of the <see cref="Queryable"/> surface: the operators, argument shapes
/// (comparers, Index/Range, user-defined conversions) and a mechanical acknowledgement test that
/// fails whenever a future .NET release adds an operator we have not covered.
/// </summary>
[TestFixture]
public class QueryableAuditTests
{
    private static readonly IQueryable<Order> Orders = TestData.OrdersQuery;
    private static readonly IQueryable<Customer> Customers = TestData.CustomersQuery;
    private static readonly IQueryable<int> Numbers = TestData.NumbersQuery;
    private static readonly IQueryable<string> Words = TestData.WordsQuery;

    private static void Query<T>(Expression<Func<T>> expression) => Executes(expression);

    private static void QueryShapeless(Expression<Func<object>> expression) => ExecutesShapeless(expression);

    [Test]
    public void UnionBy_IntersectBy_ExceptBy()
    {
        Query(() => Orders.UnionBy(Orders.Take(2), o => o.Total).Select(o => o.Id));
        Query(() => Orders.IntersectBy(new[] { 250.50m, 75.00m }, o => o.Total).Select(o => o.Id));
        Query(() => Orders.ExceptBy(new[] { 250.50m }, o => o.Total).Select(o => o.Id));
    }

    [Test]
    public void Order_and_OrderDescending()
    {
        Query(() => Numbers.Order());
        Query(() => Numbers.OrderDescending());
        Query(() => Words.Order(StringComparer.Ordinal));
    }

    [Test]
    public void AsQueryable_over_captured_collection()
    {
        var snapshot = TestData.Numbers.ToList();
        Query(() => snapshot.AsQueryable().Where(n => n > 5).Sum());
    }

    [Test]
    public void Well_known_string_comparers_survive_as_member_accesses()
    {
        Query(() => Words.Concat(new[] { "ALPHA", "Echo" }).Distinct(StringComparer.OrdinalIgnoreCase));
        Query(() => Words.Contains("BRAVO", StringComparer.OrdinalIgnoreCase));
        Query(() => Words.Contains("BRAVO", StringComparer.Ordinal));
        Query(() => Words.OrderBy(w => w, StringComparer.Ordinal).ThenByDescending(w => w, StringComparer.OrdinalIgnoreCase));
        Query(() => Words.Union(new[] { "DELTA" }, StringComparer.OrdinalIgnoreCase));
        Query(() => Words.Except(new[] { "ALPHA" }, StringComparer.OrdinalIgnoreCase));
    }

    [Test]
    public void Default_comparers_survive_as_member_accesses()
    {
        Query(() => Numbers.Distinct(EqualityComparer<int>.Default));
        Query(() => Words.GroupBy(w => w.Length, EqualityComparer<int>.Default).Select(g => g.Key + ":" + g.Count()));
        Query(() => Numbers.Max(Comparer<int>.Default));
        Query(() => Numbers.Min(Comparer<int>.Default));
    }

    [Test]
    public void GroupBy_with_comparer_over_nullable_keys()
    {
        QueryShapeless(() => Orders
            .GroupBy(o => o.Notes, StringComparer.OrdinalIgnoreCase)
            .Select(g => new { Key = g.Key ?? "(none)", Count = g.Count() }));
    }

    [Test]
    public void Index_and_range_arguments()
    {
        // C# forbids `^`/`..` literals inside expression trees (CS8791/CS8792), so Index/Range
        // reach trees in exactly two ways — both must round-trip:

        // 1. captured variables → folded into constants → JSON Index/Range converters;
        var last = ^1;
        Query(() => Numbers.ElementAt(last));

        var fromEnd = ^2;
        Query(() => Numbers.ElementAt(fromEnd));

        var window = 1..5;
        Query(() => Numbers.Take(window));

        var openEnd = 2..;
        Query(() => Numbers.Take(openEnd));

        var untilFromEnd = ..^3;
        Query(() => Numbers.Take(untilFromEnd));

        // 2. explicit construction → structural New nodes.
        Query(() => Numbers.ElementAt(new Index(2, true)));
        Query(() => Numbers.Take(new Range(new Index(2), new Index(7))));
    }

    [Test]
    public void User_defined_conversion_operator()
    {
        Executes((Expression<Func<Money, decimal>>)(m => m), Args(new Money(12.5m)));
        Executes((Expression<Func<Money, decimal>>)(m => (decimal)m), Args(new Money(9m)));
        Executes((Expression<Func<Money, bool>>)(m => m > 10m), Args(new Money(12m)), Args(new Money(5m)));
    }

    [Test]
    public void Method_with_ref_parameter()
    {
        var value = Expression.Parameter(typeof(int).MakeByRefType(), "value");
        var call = Expression.Call(typeof(StaticHelpers).GetMethod(nameof(StaticHelpers.BumpAndDouble))!, value);
        var lambda = Expression.Lambda<RefIntOp>(call, value);

        var rebuilt = (Expression<RefIntOp>)Verify.RoundTrip(lambda);

        int a = 5, b = 5;
        var expected = lambda.Compile()(ref a);
        var actual = rebuilt.Compile()(ref b);

        Assert.That(actual, Is.EqualTo(expected));
        Assert.That(b, Is.EqualTo(a));
        Assert.That(b, Is.EqualTo(6));
    }

    [Test]
    public void Debug_info_document_metadata_round_trips()
    {
        var language = new Guid("11111111-2222-3333-4444-555555555555");
        var vendor = new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var documentType = new Guid("99999999-8888-7777-6666-555555555555");

        var body = Expression.Block(
            Expression.DebugInfo(Expression.SymbolDocument("query.linq", language, vendor, documentType), 1, 1, 2, 3),
            Expression.Constant(7));

        var rebuilt = Verify.RoundTrip(Expression.Lambda<Func<int>>(body));
        var debugInfo = (DebugInfoExpression)((BlockExpression)rebuilt.Body).Expressions[0];

        Assert.That(debugInfo.Document.Language, Is.EqualTo(language));
        Assert.That(debugInfo.Document.LanguageVendor, Is.EqualTo(vendor));
        Assert.That(debugInfo.Document.DocumentType, Is.EqualTo(documentType));
    }

#if NET9_0_OR_GREATER
    [Test]
    public void CountBy_and_AggregateBy()
    {
        Query(() => Orders.CountBy(o => o.Status));
        Query(() => Orders.AggregateBy(o => o.Status, 0m, (acc, o) => acc + o.Total));
    }

    [Test]
    public void Index_operator()
    {
        Query(() => Numbers.Index().Select(pair => pair.Index + ":" + pair.Item));
    }
#endif

#if NET10_0_OR_GREATER
    [Test]
    public void LeftJoin_and_RightJoin()
    {
        QueryShapeless(() => Orders.LeftJoin(
            Customers,
            o => o.Customer.Id,
            c => c.Id,
            (o, c) => new { o.Id, Name = c != null ? c.Name : "(none)" }));

        QueryShapeless(() => Customers.RightJoin(
            Orders,
            c => c.Id,
            o => o.Customer.Id,
            (c, o) => new { o.Id, Vip = c != null && c.IsVip }));
    }

    [Test]
    public void Shuffle_round_trips_and_agrees_on_the_set()
    {
        // Shuffle is intentionally random: prove the expression round-trips and that both sides
        // produce the same SET of elements.
        var lambda = (Expression<Func<List<int>>>)(() => Numbers.Shuffle().ToList());
        var rebuilt = (Expression<Func<List<int>>>)Verify.RoundTrip(lambda);

        var expected = lambda.Compile()().OrderBy(n => n).ToList();
        var actual = rebuilt.Compile()().OrderBy(n => n).ToList();

        Assert.That(actual, Is.EqualTo(expected));
    }
#endif

    /// <summary>
    /// Mechanical completeness guard: every public Queryable operator must be acknowledged here.
    /// When a future .NET version introduces a new operator, this test fails until round-trip
    /// coverage is added for it.
    /// </summary>
    [Test]
    public void Every_queryable_operator_is_acknowledged()
    {
        string[] acknowledged =
        [
            "Aggregate", "AggregateBy", "All", "Any", "Append", "AsQueryable", "Average", "Cast",
            "Chunk", "Concat", "Contains", "Count", "CountBy", "DefaultIfEmpty", "Distinct",
            "DistinctBy", "ElementAt", "ElementAtOrDefault", "Except", "ExceptBy", "First",
            "FirstOrDefault", "GroupBy", "GroupJoin", "Index", "Intersect", "IntersectBy", "Join",
            "Last", "LastOrDefault", "LeftJoin", "LongCount", "Max", "MaxBy", "Min", "MinBy",
            "OfType", "Order", "OrderBy", "OrderByDescending", "OrderDescending", "Prepend",
            "Reverse", "RightJoin", "Select", "SelectMany", "SequenceEqual", "Shuffle", "Single",
            "SingleOrDefault", "Skip", "SkipLast", "SkipWhile", "Sum", "Take", "TakeLast",
            "TakeWhile", "ThenBy", "ThenByDescending", "Union", "UnionBy", "Where", "Zip",
        ];

        var runtimeOperators = typeof(Queryable)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Select(m => m.Name)
            .Distinct()
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        var unacknowledged = runtimeOperators.Except(acknowledged, StringComparer.Ordinal).ToList();

        Assert.That(
            unacknowledged,
            Is.Empty,
            $"Queryable operators without acknowledged round-trip coverage: {string.Join(", ", unacknowledged)}. " +
            "Add tests for them in QueryableAuditTests/LinqOperatorTests and acknowledge them here.");

        TestContext.Out.WriteLine($"Queryable operators on this runtime: {runtimeOperators.Count} — all acknowledged.");
    }
}
