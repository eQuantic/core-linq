# eQuantic.Linq.Web

REST-friendly **query-string syntax** parsed straight into typed LINQ expression trees, powered by
[eQuantic.Linq.Expressions](https://www.nuget.org/packages/eQuantic.Linq.Expressions).

```csharp
using eQuantic.Linq.Web;

// GET /orders?filter=total:gt(100),items:any(price:gt(50))&orderBy=total:desc&skip=0&take=10
var query = EntityQuery.Parse<Order>(Request.QueryString.Value);
var page = query.Apply(dbContext.Orders);

// or piecemeal:
var filtered = orders.WhereQueryString("status:in(Paid|Shipped),items.sum(price):gt(100)");
var sorted   = orders.OrderByQueryString("customer.name.toLower():asc,total:desc");
```

## Filter syntax

| Form | Example |
|------|---------|
| Comparison | `total:gt(100)` — `eq`, `neq`, `gt`, `gte`, `lt`, `lte` |
| Strings | `customer.name:ct(ali)` — `ct`, `nct`, `sw`, `ew` |
| Shorthand equality | `id:3`, `customer.isVip:true` |
| Logical | `and(…)`, `or(…)`, `not(…)` — top-level commas mean AND |
| Membership | `status:in(Paid\|Shipped)`, `id:nin(1\|2)` |
| Null tests | `deliveredAt:eq(null)`, `notes:neq(null)` |
| Collections | `items:any(price:gt(50),category:eq(Tech))`, `items:all(…)` |
| Aggregates | `items.count():gt(1)`, `items.count(price:gt(100)):gte(1)`, `items.sum(price):gt(200)`, `min`/`max`/`average` |
| Navigation | `customer.address.city:eq(Lisboa)` |
| Method segments | `notes.toLower():ct(gift)`, `customer.name.substring(0,3):eq(Ali)` |
| Quoted values | `notes:ct('Leave at')`, `name:eq('it''s')` |

Values are plain text — the engine coerces them against the member's type (`decimal`, `DateTime`,
`Guid`, enums by name, booleans, nullables). Names match case-insensitively (including
`[Column("…")]` fallback), so camelCase URLs work naturally.

- **Ordering**: `orderBy=total:desc,customer.name` (direction defaults to `asc`).
- **Paging**: `skip` / `take`.
- **Projection**: `select=id,customer.name,items.count()` — optionally aliased
  (`select=name=customer.name`) — materialized through emitted anonymous types.

## DTO → entity casting

Consumers filter by the DTO shape your API exposes; queries run on entities:

```csharp
using eQuantic.Linq.Expressions.Casting;

var cast = ExpressionCast.Create<OrderDto, Order>(o => o
    .Map(d => d.CustomerName, e => e.Customer.Name)
    .Map(d => d.Revenue, e => e.Items.Sum(i => i.Price * i.Quantity))
    .Nested<ItemDto, OrderItem>(n => n.Map(i => i.Cost, e => e.Price)));

// GET /orders?filter=revenue:gt(200)&orderBy=revenue:desc
var page = EntityQuery.Parse<OrderDto>(qs).Cast(cast).Apply(dbContext.Orders);
```

## Transport bridge

Every parsed filter is exposed as a serializable `ExpressionModel<T>`
(`EntityQuery<T>.FilterModel`, `QueryFilter.ParseModel<T>`) — and the *whole* query (filter +
sorts + paging + projection) round-trips as one JSON document:

```csharp
QueryModel<Order> document = query.ToQueryModel();            // serializable POCO
var revived = document.ToEntityQuery(options).Apply(source);  // anywhere else
```

## Semantics & hardening

- **Automatic null guards** (`NullGuardMode.Auto`, default): predicates applied to pure
  LINQ-to-objects sources get C# `?.`-style protection on deep paths; relational providers receive
  the clean tree (SQL is null-safe natively).
- **Untrusted input**: `new QueryStringOptions().UseStrictSerializer(typeof(Order), …)` locks type
  and method resolution to your contracts. Invalid syntax throws `QueryStringParseException` with
  position information.
- Parsed filters are cached per options instance (`CacheParsedFilters`).

## Learn more

[Query-string syntax reference](https://github.com/eQuantic/core-linq/blob/main/docs/query-string-syntax.md) ·
[DTO casting](https://github.com/eQuantic/core-linq/blob/main/docs/dto-casting.md) ·
[security](https://github.com/eQuantic/core-linq/blob/main/docs/security.md) ·
[all guides](https://github.com/eQuantic/core-linq/tree/main/docs)

MIT © eQuantic Tech
