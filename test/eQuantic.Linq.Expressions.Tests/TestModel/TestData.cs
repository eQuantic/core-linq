namespace eQuantic.Linq.Expressions.Tests.TestModel;

/// <summary>
/// Deterministic in-memory dataset. Queryables are cached so a serialized query root and the
/// provider-resolved root reference the same instance (enabling structural comparison).
/// </summary>
public static class TestData
{
    public static readonly List<Customer> Customers =
    [
        new()
        {
            Id = new Guid("11111111-1111-1111-1111-111111111111"),
            Name = "Alice",
            Email = "alice@example.com",
            Age = 34,
            IsVip = true,
            Address = new Address { Street = "Rua A", City = "Lisboa", Country = "PT", ZipCode = "1000-001" },
        },
        new()
        {
            Id = new Guid("22222222-2222-2222-2222-222222222222"),
            Name = "Bruno",
            Email = "bruno@example.com",
            Age = 27,
            IsVip = false,
            Address = new Address { Street = "Rua B", City = "Porto", Country = "PT", ZipCode = "4000-002" },
        },
        new()
        {
            Id = new Guid("33333333-3333-3333-3333-333333333333"),
            Name = "Carla",
            Email = null,
            Age = 41,
            IsVip = true,
            Address = new Address { Street = "Av. C", City = "São Paulo", Country = "BR", ZipCode = "01000-003" },
        },
        new()
        {
            Id = new Guid("44444444-4444-4444-4444-444444444444"),
            Name = "Diego",
            Email = "diego@example.com",
            Age = 19,
            IsVip = false,
            Address = null,
        },
    ];

    public static readonly List<Order> Orders =
    [
        new()
        {
            Id = 1,
            Reference = new Guid("aaaaaaaa-0000-0000-0000-000000000001"),
            Status = OrderStatus.Paid,
            Tags = OrderTags.Gift | OrderTags.Express,
            Total = 250.50m,
            Discount = 10.5m,
            CreatedAt = new DateTime(2026, 1, 10, 8, 30, 0, DateTimeKind.Utc),
            DeliveredAt = new DateTime(2026, 1, 15, 14, 0, 0, DateTimeKind.Utc),
            Notes = "Leave at the door",
            Priority = 2,
            Customer = Customers[0],
            Items =
            [
                new OrderItem { Id = 1, Product = "Keyboard", Category = "Tech", Price = 120.00m, Quantity = 1 },
                new OrderItem { Id = 2, Product = "Mouse", Category = "Tech", Price = 65.25m, Quantity = 2 },
            ],
        },
        new()
        {
            Id = 2,
            Reference = new Guid("aaaaaaaa-0000-0000-0000-000000000002"),
            Status = OrderStatus.New,
            Tags = OrderTags.None,
            Total = 39.90m,
            Discount = null,
            CreatedAt = new DateTime(2026, 2, 2, 10, 0, 0, DateTimeKind.Utc),
            DeliveredAt = null,
            Notes = null,
            Priority = 0,
            Customer = Customers[1],
            Items =
            [
                new OrderItem { Id = 3, Product = "Book", Category = "Culture", Price = 39.90m, Quantity = 1 },
            ],
        },
        new()
        {
            Id = 3,
            Reference = new Guid("aaaaaaaa-0000-0000-0000-000000000003"),
            Status = OrderStatus.Shipped,
            Tags = OrderTags.International | OrderTags.Fragile,
            Total = 1899.00m,
            Discount = 150m,
            CreatedAt = new DateTime(2026, 3, 20, 18, 45, 0, DateTimeKind.Utc),
            DeliveredAt = null,
            Notes = "Fragile — glass",
            Priority = 5,
            Customer = Customers[2],
            Items =
            [
                new OrderItem { Id = 4, Product = "Monitor", Category = "Tech", Price = 899.00m, Quantity = 2 },
                new OrderItem { Id = 5, Product = "Vase", Category = "Home", Price = 101.00m, Quantity = 1 },
            ],
        },
        new()
        {
            Id = 4,
            Reference = new Guid("aaaaaaaa-0000-0000-0000-000000000004"),
            Status = OrderStatus.Delivered,
            Tags = OrderTags.Gift,
            Total = 75.00m,
            Discount = null,
            CreatedAt = new DateTime(2025, 12, 24, 9, 15, 0, DateTimeKind.Utc),
            DeliveredAt = new DateTime(2025, 12, 26, 11, 30, 0, DateTimeKind.Utc),
            Notes = "gift wrap please",
            Priority = 1,
            Customer = Customers[0],
            Items =
            [
                new OrderItem { Id = 6, Product = "Perfume", Category = "Beauty", Price = 75.00m, Quantity = 1 },
            ],
        },
        new()
        {
            Id = 5,
            Reference = new Guid("aaaaaaaa-0000-0000-0000-000000000005"),
            Status = OrderStatus.Cancelled,
            Tags = OrderTags.Express,
            Total = 320.10m,
            Discount = 20m,
            CreatedAt = new DateTime(2026, 4, 1, 16, 5, 0, DateTimeKind.Utc),
            DeliveredAt = null,
            Notes = "Customer cancelled",
            Priority = 3,
            Customer = Customers[3],
            Items = [],
        },
        new()
        {
            Id = 6,
            Reference = new Guid("aaaaaaaa-0000-0000-0000-000000000006"),
            Status = OrderStatus.Paid,
            Tags = OrderTags.None,
            Total = 250.50m,
            Discount = null,
            CreatedAt = new DateTime(2026, 5, 5, 12, 0, 0, DateTimeKind.Utc),
            DeliveredAt = null,
            Notes = "duplicate total on purpose",
            Priority = 2,
            Customer = Customers[2],
            Items =
            [
                new OrderItem { Id = 7, Product = "Chair", Category = "Home", Price = 250.50m, Quantity = 1 },
            ],
        },
    ];

    public static readonly List<int> Numbers = [5, 3, 8, 1, 9, 3, 7, 2, 8, 4];

    public static readonly List<string> Words = ["alpha", "bravo", "charlie", "delta", "echo", "bravo"];

    public static readonly List<object> Mixed = [1, "two", 3.5, new OrderItem { Id = 99, Product = "Odd", Price = 1m, Quantity = 1 }, 4, "five"];

    public static readonly IQueryable<Order> OrdersQuery = Orders.AsQueryable();
    public static readonly IQueryable<Customer> CustomersQuery = Customers.AsQueryable();
    public static readonly IQueryable<OrderItem> ItemsQuery = Orders.SelectMany(o => o.Items).ToList().AsQueryable();
    public static readonly IQueryable<int> NumbersQuery = Numbers.AsQueryable();
    public static readonly IQueryable<string> WordsQuery = Words.AsQueryable();
    public static readonly IQueryable<object> MixedQuery = Mixed.AsQueryable();

    public static IQueryable? GetQueryable(Type elementType)
    {
        if (elementType == typeof(Order))
        {
            return OrdersQuery;
        }

        if (elementType == typeof(Customer))
        {
            return CustomersQuery;
        }

        if (elementType == typeof(OrderItem))
        {
            return ItemsQuery;
        }

        if (elementType == typeof(int))
        {
            return NumbersQuery;
        }

        if (elementType == typeof(string))
        {
            return WordsQuery;
        }

        if (elementType == typeof(object))
        {
            return MixedQuery;
        }

        return null;
    }
}
