using System.Text.Json;
using eQuantic.Linq.Expressions.Casting;
using eQuantic.Linq.Expressions.Tests.Support;
using eQuantic.Linq.Expressions.Tests.TestModel;
using eQuantic.Linq.Web;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace eQuantic.Linq.Expressions.Tests;

/// <summary>
/// The credibility test: rebuilt expressions must TRANSLATE through a real relational provider
/// (SQLite via EF Core), not just execute over in-memory objects.
/// </summary>
[TestFixture]
public class EfCoreIntegrationTests
{
    private sealed class ShopContext(DbContextOptions<ShopContext> options) : DbContext(options)
    {
        public DbSet<Order> Orders => Set<Order>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>().OwnsOne(c => c.Address);
            modelBuilder.Entity<Order>().HasOne(o => o.Customer).WithMany();
            modelBuilder.Entity<Order>().HasMany(o => o.Items).WithOne();
            modelBuilder.Entity<OrderItem>();
        }
    }

    private SqliteConnection _connection = null!;
    private ShopContext _db = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _db = new ShopContext(new DbContextOptionsBuilder<ShopContext>().UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();

        // Deep-clone the shared dataset (shared Customer instances must become single tracked rows).
        var clones = JsonSerializer.Deserialize<List<Order>>(JsonSerializer.Serialize(TestData.Orders))!;
        var customers = new Dictionary<Guid, Customer>();
        foreach (var order in clones)
        {
            if (customers.TryGetValue(order.Customer.Id, out var existing))
            {
                order.Customer = existing;
            }
            else
            {
                customers[order.Customer.Id] = order.Customer;
            }
        }

        _db.AddRange(clones);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private List<int> Ids(IQueryable<Order> query) => query.Select(o => o.Id).OrderBy(id => id).ToList();

    [Test]
    public void Query_string_filters_translate_to_sql()
    {
        var query = _db.Orders.WhereQueryString(
            "and(total:gt(100),status:in(Paid|Shipped),customer.name.toUpper():sw(CAR))");

        Assert.That(Ids(query), Is.EqualTo(new[] { 3, 6 }));
    }

    [Test]
    public void Nested_any_and_aggregates_translate()
    {
        Assert.That(Ids(_db.Orders.WhereQueryString("items:any(price:gt(500))")), Is.EqualTo(new[] { 3 }));
        Assert.That(Ids(_db.Orders.WhereQueryString("items.count():gt(1)")), Is.EqualTo(new[] { 1, 3 }));
    }

    [Test]
    public void Serialized_round_trip_predicates_translate()
    {
        var serializer = Verify.Serializer();
        // int aggregation: decimal SUM is not translatable by the SQLite provider on EF 8
        var json = serializer.ToJson(serializer.ToModel<Order, bool>(o => o.Items.Sum(i => i.Quantity) > 2));
        var predicate = serializer.ModelFromJson<Order>(json).ToPredicate(serializer);

        Assert.That(Ids(_db.Orders.Where(predicate)), Is.EqualTo(new[] { 1, 3 }));
    }

    [Test]
    public void Sorting_paging_and_dto_cast_translate()
    {
        // computed-decimal ordering is not translatable on EF 8 + SQLite; the flattened
        // navigation filter + sort + paging still prove the cast pipeline end to end.
        var cast = ExpressionCast.Create<OrderDto, Order>(o => o
            .Map(d => d.CustomerName, e => e.Customer.Name));

        var query = EntityQuery
            .Parse<OrderDto>("?filter=customerName:ct(a)&orderBy=id:desc&skip=0&take=2")
            .Cast(cast)
            .Apply(_db.Orders);

        Assert.That(query.Select(o => o.Id).ToList(), Is.EqualTo(new[] { 6, 3 }));
    }

    [Test]
    public void Anonymous_projection_materializes_through_ef()
    {
        var results = EntityQuery
            .Parse<Order>("?filter=id:lte(2)&orderBy=id&select=id,customer.name")
            .ApplyWithSelection(_db.Orders)
            .Cast<object>()
            .ToList();

        Assert.That(results.Select(r => r.ToString()), Is.EqualTo(new[]
        {
            "{ Id = 1, CustomerName = Alice }",
            "{ Id = 2, CustomerName = Bruno }",
        }));
    }
}
