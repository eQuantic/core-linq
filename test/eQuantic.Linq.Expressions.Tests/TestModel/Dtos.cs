using System.ComponentModel.DataAnnotations.Schema;

namespace eQuantic.Linq.Expressions.Tests.TestModel;

/// <summary>
/// The shape an API would expose: slightly different from the data model on purpose —
/// flattened navigations, computed values, renamed members and legacy columns.
/// </summary>
public class OrderDto
{
    public int Id { get; set; }

    public decimal Total { get; set; }

    public OrderStatus Status { get; set; }

    /// <summary>Mapped: <c>o.Status.ToString()</c>.</summary>
    public string StatusName { get; set; } = string.Empty;

    /// <summary>Mapped: <c>o.Customer.Name</c> (flattened navigation).</summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>Mapped: <c>o.Items.Sum(i =&gt; i.Price * i.Quantity)</c> (computed aggregate).</summary>
    public decimal Revenue { get; set; }

    /// <summary>Mapped: <c>o.Customer.Name + " #" + o.Id</c> (concatenation).</summary>
    public string Display { get; set; } = string.Empty;

    /// <summary>Auto-mapped by name; element pair configured via Nested&lt;ItemDto, OrderItem&gt;.</summary>
    public List<ItemDto> Items { get; set; } = [];

    /// <summary>Column fallback: the DTO name differs, the [Column] name matches the entity member.</summary>
    [Column("Total")]
    public decimal Amount { get; set; }

    /// <summary>No counterpart on the entity — using it must raise a clear cast error.</summary>
    public string Legacy { get; set; } = string.Empty;

    /// <summary>Same name as the entity member but an incompatible type (entity side is int).</summary>
    public string Priority { get; set; } = string.Empty;
}

public class ItemDto
{
    public string Product { get; set; } = string.Empty;

    /// <summary>Mapped: <c>i.Price</c>.</summary>
    public decimal Cost { get; set; }

    public int Quantity { get; set; }
}
