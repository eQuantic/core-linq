namespace eQuantic.Linq.Expressions.Tests.TestModel;

public enum OrderStatus
{
    New,
    Paid,
    Shipped,
    Delivered,
    Cancelled,
}

[Flags]
public enum OrderTags
{
    None = 0,
    Gift = 1,
    Express = 2,
    Fragile = 4,
    International = 8,
}

public class Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
}

public class Customer
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }

    // Column attribute on purpose: exercises engine-level column fallback resolution.
    [System.ComponentModel.DataAnnotations.Schema.Column("customer_age")]
    public int Age { get; set; }

    public bool IsVip { get; set; }
    public Address? Address { get; set; }
}

public class OrderItem
{
    public OrderItem()
    {
    }

    public OrderItem(string product, decimal price, int quantity)
    {
        Product = product;
        Price = price;
        Quantity = quantity;
    }

    public int Id { get; set; }
    public string Product { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public Guid Reference { get; set; }
    public OrderStatus Status { get; set; }
    public OrderTags Tags { get; set; }
    public decimal Total { get; set; }
    public decimal? Discount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? Notes { get; set; }
    public Customer Customer { get; set; } = new();
    public List<OrderItem> Items { get; set; } = [];

    // Public field on purpose: exercises field-access serialization.
    public int Priority;
}

/// <summary>Struct with user-defined operators to exercise the BinaryExpression.Method path.</summary>
public readonly struct Money : IEquatable<Money>
{
    [System.Text.Json.Serialization.JsonConstructor]
    public Money(decimal amount)
    {
        Amount = amount;
    }

    public decimal Amount { get; }

    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);

    public static Money operator -(Money value) => new(-value.Amount);

    public static implicit operator decimal(Money money) => money.Amount;

    public static bool operator >(Money left, Money right) => left.Amount > right.Amount;

    public static bool operator <(Money left, Money right) => left.Amount < right.Amount;

    public bool Equals(Money other) => Amount == other.Amount;

    public override bool Equals(object? obj) => obj is Money other && Equals(other);

    public override int GetHashCode() => Amount.GetHashCode();
}

/// <summary>Instance class with generic method and indexer for call-shape coverage.</summary>
public class Calculator
{
    public int Seed { get; set; }

    public int this[int a, int b] => Seed + (a * b);

    public T Pick<T>(bool first, T a, T b) => first ? a : b;

    public int Sum(params int[] values) => values.Sum() + Seed;
}

public static class StaticHelpers
{
    public static readonly string Marker = "marker";

    public static T Echo<T>(T value) => value;

    public static int Twice(int value) => value * 2;

    public static int BumpAndDouble(ref int value)
    {
        value += 1;
        return value * 2;
    }
}

public static class StringDomainExtensions
{
    public static bool ContainsIgnoreCase(this string source, string term) =>
        source.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
}

/// <summary>Delegate with a by-ref parameter to exercise by-ref parameters and custom delegate types.</summary>
public delegate int RefIntOp(ref int value);
