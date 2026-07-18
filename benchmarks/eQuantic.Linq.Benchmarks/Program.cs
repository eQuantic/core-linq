using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using eQuantic.Linq.Expressions;
using eQuantic.Linq.Web;

BenchmarkRunner.Run<SerializationBenchmarks>(args: args);

public class Order
{
    public int Id { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "";
    public Customer Customer { get; set; } = new();
    public List<OrderItem> Items { get; set; } = [];
}

public class Customer { public string Name { get; set; } = ""; public bool IsVip { get; set; } }

public class OrderItem { public string Product { get; set; } = ""; public decimal Price { get; set; } public int Quantity { get; set; } }

[MemoryDiagnoser]
public class SerializationBenchmarks
{
    private static readonly Expression<Func<Order, bool>> Predicate =
        o => o.Total > 100m && o.Customer.IsVip && o.Items.Any(i => i.Price * i.Quantity > 50m);

    private const string Filter = "and(total:gt(100),customer.isVip:true,items:any(price:gt(50)))";

    private readonly ExpressionSerializer _serializer = new();
    private readonly QueryStringOptions _cached = new();
    private readonly QueryStringOptions _uncached = new() { CacheParsedFilters = false };
    private string _json = null!;
    private string _leanJson = null!;

    [GlobalSetup]
    public void Setup()
    {
        _json = _serializer.ToJson(Predicate);
        _leanJson = _serializer.ToJson(_serializer.ToModel<Order, bool>(Predicate));
        _ = QueryFilter.Parse<Order>(Filter, _cached); // warm the cache
    }

    [Benchmark]
    public string ToJson_FullFidelity() => _serializer.ToJson(Predicate);

    [Benchmark]
    public object FromJson_FullFidelity() => _serializer.FromJson<Func<Order, bool>>(_json);

    [Benchmark]
    public object LeanModel_ToPredicate() => _serializer.ModelFromJson<Order>(_leanJson).ToPredicate(_serializer);

    [Benchmark]
    public object QueryString_Parse_Cold() => QueryFilter.Parse<Order>(Filter, _uncached);

    [Benchmark]
    public object QueryString_Parse_Cached() => QueryFilter.Parse<Order>(Filter, _cached);
}
