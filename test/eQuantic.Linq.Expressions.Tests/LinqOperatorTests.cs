using System.Linq.Expressions;
using eQuantic.Linq.Expressions.Tests.Support;
using eQuantic.Linq.Expressions.Tests.TestModel;
using static eQuantic.Linq.Expressions.Tests.Support.Verify;

namespace eQuantic.Linq.Expressions.Tests;

/// <summary>
/// Every LINQ operator serialized as a full IQueryable pipeline (query root included), rebuilt on the
/// "other side" through <see cref="ExpressionSerializerOptions.QueryRootProvider"/> and executed on both
/// sides with results compared. This is the operator support matrix.
/// </summary>
[TestFixture]
public class LinqOperatorTests
{
    private static readonly IQueryable<Order> Orders = TestData.OrdersQuery;
    private static readonly IQueryable<Customer> Customers = TestData.CustomersQuery;
    private static readonly IQueryable<OrderItem> Items = TestData.ItemsQuery;
    private static readonly IQueryable<int> Numbers = TestData.NumbersQuery;
    private static readonly IQueryable<string> Words = TestData.WordsQuery;
    private static readonly IQueryable<object> Mixed = TestData.MixedQuery;

    private static void Query<T>(Expression<Func<T>> expression) => Executes(expression);

    private static void QueryShapeless(Expression<Func<object>> expression) => ExecutesShapeless(expression);

    [Test]
    public void Where() => Query(() => Orders.Where(o => o.Total > 100m && o.Status != OrderStatus.Cancelled));

    [Test]
    public void Where_with_index() => Query(() => Numbers.Where((n, i) => i % 2 == 0 && n > 2));

    [Test]
    public void Select() => Query(() => Orders.Select(o => o.Customer.Name));

    [Test]
    public void Select_with_index() => Query(() => Numbers.Select((n, i) => n * 10 + i));

    [Test]
    public void Select_anonymous_projection() =>
        QueryShapeless(() => Orders.Select(o => new { o.Id, o.Status, Customer = o.Customer.Name, ItemCount = o.Items.Count }));

    [Test]
    public void SelectMany() => Query(() => Orders.SelectMany(o => o.Items));

    [Test]
    public void SelectMany_with_result_selector() =>
        Query(() => Orders.SelectMany(o => o.Items, (o, i) => o.Id + ":" + i.Product));

    [Test]
    public void OrderBy_ThenBy_chains() =>
        Query(() => Orders
            .OrderBy(o => o.Status)
            .ThenByDescending(o => o.Total)
            .ThenBy(o => o.Id)
            .Select(o => o.Id));

    [Test]
    public void OrderByDescending() => Query(() => Numbers.OrderByDescending(n => n));

    [Test]
    public void Skip_and_Take() => Query(() => Orders.OrderBy(o => o.Id).Skip(2).Take(3).Select(o => o.Id));

    [Test]
    public void SkipWhile_and_TakeWhile() =>
        Query(() => Numbers.TakeWhile(n => n > 2).Concat(Numbers.SkipWhile(n => n > 2)));

    [Test]
    public void SkipLast_and_TakeLast() => Query(() => Numbers.SkipLast(3).Concat(Numbers.TakeLast(2)));

    [Test]
    public void Distinct() => Query(() => Numbers.Distinct());

    [Test]
    public void DistinctBy() => Query(() => Orders.DistinctBy(o => o.Total).Select(o => o.Id));

    [Test]
    public void Union_Intersect_Except() =>
        Query(() => Numbers.Union(new[] { 99, 5 }).Intersect(new[] { 99, 5, 8, 1 }).Except(new[] { 1 }));

    [Test]
    public void Concat() => Query(() => Words.Concat(Words.Take(2)));

    [Test]
    public void Reverse() => Query(() => Numbers.Reverse());

    [Test]
    public void Chunk() => Query(() => Numbers.Chunk(3));

    [Test]
    public void Zip_with_selector() => Query(() => Numbers.Zip(Words, (n, w) => n + ":" + w));

    [Test]
    public void Append_and_Prepend() => Query(() => Numbers.Append(100).Prepend(-1));

    [Test]
    public void DefaultIfEmpty() =>
        Query(() => Orders.Where(o => o.Total > 999999m).Select(o => o.Id).DefaultIfEmpty(-1));

    [Test]
    public void Cast_and_OfType()
    {
        Query(() => Mixed.OfType<int>());
        Query(() => Mixed.OfType<string>());
        Query(() => Numbers.Cast<object>());
    }

    [Test]
    public void GroupBy_raw() => Query(() => Orders.GroupBy(o => o.Status));

    [Test]
    public void GroupBy_projected() =>
        QueryShapeless(() => Orders
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), Total = g.Sum(o => o.Total) }));

    [Test]
    public void GroupBy_with_element_selector() =>
        QueryShapeless(() => Orders
            .GroupBy(o => o.Customer.IsVip, o => o.Total)
            .Select(g => new { Vip = g.Key, Max = g.Max() }));

    [Test]
    public void GroupBy_with_result_selector() =>
        QueryShapeless(() => Orders.GroupBy(
            o => o.Status,
            (status, group) => new { Status = status, Items = group.SelectMany(o => o.Items).Count() }));

    [Test]
    public void GroupBy_anonymous_composite_key() =>
        QueryShapeless(() => Orders
            .GroupBy(o => new { o.Status, Vip = o.Customer.IsVip })
            .Select(g => new { g.Key.Status, g.Key.Vip, Count = g.Count() }));

    [Test]
    public void Join() =>
        QueryShapeless(() => Orders.Join(
            Customers,
            o => o.Customer.Id,
            c => c.Id,
            (o, c) => new { o.Id, c.Name }));

    [Test]
    public void Join_with_anonymous_keys() =>
        QueryShapeless(() => Orders.Join(
            Customers,
            o => new { o.Customer.Id, Vip = o.Customer.IsVip },
            c => new { c.Id, Vip = c.IsVip },
            (o, c) => new { o.Id, Customer = c.Name }));

    [Test]
    public void GroupJoin() =>
        QueryShapeless(() => Customers.GroupJoin(
            Orders,
            c => c.Id,
            o => o.Customer.Id,
            (c, orders) => new { c.Name, OrderCount = orders.Count(), Total = orders.Sum(o => o.Total) }));

    [Test]
    public void Count_and_LongCount()
    {
        Query(() => Orders.Count());
        Query(() => Orders.Count(o => o.Status == OrderStatus.Paid));
        Query(() => Orders.LongCount());
        Query(() => Orders.LongCount(o => o.Total > 100m));
    }

    [Test]
    public void Any_and_All()
    {
        Query(() => Orders.Any());
        Query(() => Orders.Any(o => o.Total > 1000m));
        Query(() => Orders.Any(o => o.Total > 999999m));
        Query(() => Orders.All(o => o.Total > 10m));
        Query(() => Orders.All(o => o.Status == OrderStatus.Paid));
    }

    [Test]
    public void Contains()
    {
        Query(() => Numbers.Contains(8));
        Query(() => Numbers.Contains(999));
        Query(() => Words.Contains("bravo"));
    }

    [Test]
    public void SequenceEqual() => Query(() => Numbers.SequenceEqual(Numbers));

    [Test]
    public void First_variants()
    {
        Query(() => Orders.OrderBy(o => o.Id).First());
        Query(() => Orders.First(o => o.Id == 3));
        Query(() => Orders.FirstOrDefault(o => o.Total > 999999m));
        Query(() => Numbers.FirstOrDefault(n => n > 100, -1));
    }

    [Test]
    public void Single_variants()
    {
        Query(() => Orders.Single(o => o.Id == 3));
        Query(() => Orders.SingleOrDefault(o => o.Id == 999));
        Query(() => Orders.Single(o => o.Status == OrderStatus.Paid)); // throws: two matches — must throw identically on both sides
    }

    [Test]
    public void Last_variants()
    {
        Query(() => Orders.OrderBy(o => o.Id).Last());
        Query(() => Orders.OrderBy(o => o.Id).Last(o => o.Status == OrderStatus.Paid));
        Query(() => Orders.OrderBy(o => o.Id).LastOrDefault(o => o.Id > 999));
    }

    [Test]
    public void ElementAt_variants()
    {
        Query(() => Numbers.ElementAt(2));
        Query(() => Numbers.ElementAtOrDefault(99));
        Query(() => Orders.OrderBy(o => o.Id).ElementAt(1));
    }

    [Test]
    public void Sum_variants()
    {
        Query(() => Numbers.Sum());
        Query(() => Orders.Sum(o => o.Total));
        Query(() => Orders.Sum(o => o.Discount)); // nullable decimal
        Query(() => Orders.SelectMany(o => o.Items).Sum(i => i.Price * i.Quantity));
        Query(() => Numbers.Sum(n => (double)n / 3));
    }

    [Test]
    public void Average_variants()
    {
        Query(() => Numbers.Average());
        Query(() => Orders.Average(o => o.Total));
        Query(() => Orders.Average(o => o.Discount));
    }

    [Test]
    public void Min_Max_variants()
    {
        Query(() => Numbers.Min());
        Query(() => Numbers.Max());
        Query(() => Orders.Min(o => o.Total));
        Query(() => Orders.Max(o => o.CreatedAt));
        Query(() => Orders.Min(o => o.Discount));
        Query(() => Orders.Max(o => o.Customer.Name));
    }

    [Test]
    public void MinBy_and_MaxBy()
    {
        Query(() => Orders.MinBy(o => o.Total));
        Query(() => Orders.MaxBy(o => o.Total));
    }

    [Test]
    public void Aggregate_variants()
    {
        Query(() => Numbers.Aggregate((a, b) => a + b));
        Query(() => Numbers.Aggregate(100, (acc, n) => acc + (n * 2)));
        Query(() => Numbers.Aggregate(0, (acc, n) => acc + n, total => total / 2.0));
        Query(() => Words.Aggregate((a, b) => a + "," + b));
    }

    [Test]
    public void Parameterized_query()
    {
        var lambda = (Expression<Func<decimal, OrderStatus, List<int>>>)((min, status) =>
            Orders.Where(o => o.Total >= min && o.Status == status).Select(o => o.Id).ToList());

        Executes(lambda, Args(100m, OrderStatus.Paid), Args(0m, OrderStatus.Cancelled), Args(9999m, OrderStatus.New));
    }

    [Test]
    public void Nested_subqueries_over_navigation()
    {
        Query(() => Orders.Where(o => o.Items.Any(i => i.Category == "Tech")).Select(o => o.Id));
        Query(() => Orders.Where(o => o.Items.Count(i => i.Price > 50m) > 1).Select(o => o.Id));
        Query(() => Orders.Select(o => o.Items.Sum(i => i.Price * i.Quantity)));
        Query(() => Orders.Where(o => o.Items.Select(i => i.Category).Distinct().Count() > 1).Select(o => o.Id));
    }

    [Test]
    public void Subquery_against_second_root()
    {
        // Correlated subquery hitting ANOTHER queryable root.
        Query(() => Orders
            .Where(o => Customers.Any(c => c.Id == o.Customer.Id && c.IsVip))
            .Select(o => o.Id));
    }

    [Test]
    public void Composite_mega_pipeline()
    {
        QueryShapeless(() => Orders
            .Where(o => o.Status != OrderStatus.Cancelled && o.Items.Any())
            .SelectMany(o => o.Items, (o, i) => new { o.Status, Vip = o.Customer.IsVip, Item = i })
            .Where(x => x.Item.Price > 30m)
            .GroupBy(x => new { x.Status, x.Vip })
            .Select(g => new
            {
                g.Key.Status,
                g.Key.Vip,
                Count = g.Count(),
                Revenue = g.Sum(x => x.Item.Price * x.Item.Quantity),
                Top = g.OrderByDescending(x => x.Item.Price).First().Item.Product,
            })
            .OrderByDescending(r => r.Revenue)
            .ThenBy(r => r.Status)
            .Take(5));
    }

    [Test]
    public void Query_over_items_root() => Query(() => Items.Where(i => i.Category == "Tech").Sum(i => i.Price));
}
