# DTO → entity casting

## The problem

A REST API that respects its consumers exposes **DTOs** and mirrors its filters on the same
properties the GET endpoint returns. The consumer knows this:

```csharp
public class OrderDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; }   // flattened from Order.Customer.Name
    public decimal Revenue { get; set; }       // computed from the items
    public List<ItemDto> Items { get; set; }   // ItemDto.Cost ↔ OrderItem.Price
}
```

…but the query must run on **entities**. `ExpressionCast` rewrites expressions authored over the
DTO shape onto the data model — including *computed* mappings.

## Configure once, reuse forever

```csharp
using eQuantic.Linq.Expressions.Casting;

public static readonly ExpressionCast<OrderDto, Order> ToEntity =
    ExpressionCast.Create<OrderDto, Order>(options => options
        .Map(d => d.CustomerName, e => e.Customer.Name)                       // navigation
        .Map(d => d.Revenue, e => e.Items.Sum(i => i.Price * i.Quantity))     // arithmetic
        .Map(d => d.Display, e => e.Customer.Name + " #" + e.Id)              // concatenation
        .Map(d => d.StatusName, e => e.Status.ToString())                     // shape conversion
        .Nested<ItemDto, OrderItem>(n => n.Map(i => i.Cost, e => e.Price)));  // element pair
```

The cast is thread-safe; build it once (static field, DI singleton).

**What you don't map, matches by name** (case-insensitive; `[Column("…")]` names participate —
disable per cast with `options.ColumnFallback = false`, or require explicit maps with
`options.AutoMapByName = false`).

## What rewriting really does

```csharp
Expression<Func<OrderDto, bool>> fromClient = d => d.Revenue > 200m && d.Items.Any(i => i.Cost > 50m);

Expression<Func<Order, bool>> where = ToEntity.Predicate(fromClient);
// e => e.Items.Sum(i => i.Price * i.Quantity) > 200m && e.Items.Any(i => i.Price > 50m)
```

Note the second clause: the *generic method itself* was re-bound — `Any<ItemDto>` became
`Any<OrderItem>` by type unification, the lambda parameter changed type and its body was remapped.
Anonymous projections are re-emitted when argument types change. Value types must be preserved by
each map (express conversions inside the target expression, e.g. `e => e.Status.ToString()`);
anything unmapped or incompatible fails with an `ExpressionCastException` naming the exact
`Map(...)` to add.

## The full REST pipeline

```csharp
// GET /orders?filter=revenue:gt(200),items:any(cost:gt(50))&orderBy=revenue:desc&take=10
var page = EntityQuery.Parse<OrderDto>(Request.QueryString.Value)  // parsed against the DTO shape
    .Cast(ToEntity)                                                // rewritten onto Order
    .Apply(db.Orders);                                             // translated by EF
```

Filter, sorts and projection all cross the cast; paging is preserved; the resulting
`FilterModel` is re-encoded over the entity, so forwarding it downstream leaks nothing about the DTO.

## Coming back: entities → DTOs

The same mappings define the reverse materializer:

```csharp
var toDto = ToEntity.Project();     // Expression<Func<Order, OrderDto>>
var dtos = db.Orders.Where(where).Select(toDto).ToList();
```

Explicit maps are inlined (`Revenue` is computed!), matching members are copied, registered nested
collection pairs project element-wise (`Items.Select(i => new ItemDto { Cost = i.Price, … }).ToList()`),
and members with no counterpart are skipped.

Serialized payloads cast too: `ToEntity.Model(dtoModel)` turns an `ExpressionModel<OrderDto>`
received over the wire into an `ExpressionModel<Order>` — querystring → JSON → cast → entity, end
to end.

Next: [Security →](security.md)
