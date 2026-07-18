# eQuantic.Linq

Four packages built on one engine:

| Package | Purpose |
|---------|---------|
| **eQuantic.Linq.Expressions** | Convert any .NET expression tree into a structured, serialization-friendly model (JSON) and rebuild the exact expression back. Includes DTO→entity casting. |
| **eQuantic.Linq.Web** | Query-string syntax (`filter`, `orderBy`, `skip`, `take`, `select`) parsed straight into typed expression trees through the same engine. |
| **eQuantic.Linq.Specification** | The classic composable specification pattern, plus `ExpressionModelSpecification` for serialized expression payloads. |
| **eQuantic.Linq.Web.Specification** | `QueryStringSpecification` — REST filter query strings as composable specifications. |

# eQuantic.Linq.Expressions

Convert **any .NET expression tree** into a fully structured, serialization-friendly model (JSON) — and rebuild the exact expression back, with full fidelity.

```
Expression<Func<Order, bool>>  ⇄  ExpressionNode model  ⇄  JSON
```

- **Every `ExpressionType`** is supported (84 of 85 — see [coverage](#coverage)): from simple binaries to blocks, loops, switch, try/catch, goto and labels.
- **Every LINQ operator**: `Where`, `Select`, `SelectMany`, `GroupBy`, `Join`, `GroupJoin`, `OrderBy`/`ThenBy`, `Skip`/`Take`, `Distinct`, `Union`, `Aggregate`, `Count`, `Sum`, `Min`/`Max`, `Any`/`All`, `Zip`, `Chunk`… serialized as whole `IQueryable` pipelines, query root included.
- **Closures are handled**: captured locals, `this` references and compiler display classes are folded into portable constants before serialization.
- **Anonymous types travel**: projections like `x => new { x.Id, Total = ... }` are described structurally and re-materialized on the other side with matching equality semantics (safe for `GroupBy`/`Distinct` keys).
- **Root-anchored typed model** (`ExpressionModel<TRoot>`): lean payloads where everything below the root entity is *inferred* — lean enough for a front-end to write filters by hand.
- Targets `netstandard2.0`, `net8.0` and `net10.0`.

## Install

```bash
dotnet add package eQuantic.Linq.Expressions
```

## Quick start — full fidelity

```csharp
using eQuantic.Linq.Expressions;

Expression<Func<Order, bool>> filter = o => o.Total > minimum && o.Items.Any(i => i.Price > 50m);

// serialize (captured `minimum` is folded into a constant automatically)
string json = ExpressionSerializer.Default.ToJson(filter);

// ...transport, store, audit...

// rebuild the exact expression
var rebuilt = ExpressionSerializer.Default.FromJson<Func<Order, bool>>(json);
var results = orders.Where(rebuilt.Compile());
```

The intermediate model is a plain object graph you can inspect or build programmatically:

```csharp
ExpressionNode node = ExpressionSerializer.Default.ToNode(filter);
// node is a LambdaNode { Parameters, Body: BinaryNode { NodeType: AndAlso, ... } }
```

## The typed model — `ExpressionModel<TRoot>`

Anchor the payload on a root entity and everything below it is inferred: parameter types, member
owners, constant types (from the sibling operand, the bound method parameter, the assigned member),
generic method arguments (by unification) and delegate types.

```csharp
var model = ExpressionModel<Order>.From(o => o.Total > 100m);
string json = model.ToJson();
```

```json
{
  "parameters": [{ "id": 0, "name": "o" }],
  "body": {
    "$type": "binary",
    "nodeType": "GreaterThan",
    "left":  { "$type": "member", "member": { "name": "Total" }, "expression": { "$type": "parameter", "id": 0, "name": "o" } },
    "right": { "$type": "constant", "value": 100 }
  }
}
```

```csharp
Expression<Func<Order, bool>> predicate = ExpressionModel<Order>.FromJson(json).ToPredicate();
```

Payloads are lean enough to be **written by hand** (or by a front-end). Member and method names are
matched case-insensitively, enum constants accept strings, and extension methods can be called in
instance style — the binder resolves `Enumerable`/`Queryable` by generic unification:

```json
{
  "body": {
    "$type": "call",
    "method": { "name": "any" },
    "object": { "$type": "member", "member": { "name": "items" }, "expression": { "$type": "parameter" } },
    "arguments": [{
      "$type": "lambda",
      "parameters": [{ "name": "i" }],
      "body": {
        "$type": "binary",
        "nodeType": "GreaterThan",
        "left":  { "$type": "member", "member": { "name": "price" }, "expression": { "$type": "parameter", "name": "i" } },
        "right": { "$type": "constant", "value": 50 }
      }
    }]
  }
}
```

```csharp
var predicate = serializer.ModelFromJson<Order>(json).ToPredicate();
// o => o.Items.Any(i => i.Price > 50)  — i : OrderItem inferred, Enumerable.Any<OrderItem> bound
```

Need self-contained payloads instead? Use `TypeInfoMode.Full`:

```csharp
var model = serializer.ToModel<Order, bool>(filter, TypeInfoMode.Full);
```

## Whole-query pipelines

`IQueryable` roots inside an expression are serialized as re-bindable placeholders. On the other
side, a `QueryRootProvider` supplies the local data source — so a complete query composed on a
client can be executed on a server:

```csharp
// client
var query = orders.AsQueryable()
    .Where(o => o.Status != OrderStatus.Cancelled)
    .GroupBy(o => o.Customer.Id)
    .Select(g => new { Customer = g.Key, Revenue = g.Sum(o => o.Total) })
    .OrderByDescending(r => r.Revenue)
    .Take(10);

string json = serializer.ToJson(query.Expression);

// server
var server = new ExpressionSerializer(new ExpressionSerializerOptions
{
    QueryRootProvider = elementType => database.GetQueryable(elementType),
});

var expression = server.FromJson(json);
var results = database.Orders.Provider.CreateQuery(expression);
```

## Security

Deserializing type names is a classic attack surface. For untrusted payloads, enable strict
resolution and register the contracts you expect:

```csharp
var options = new ExpressionSerializerOptions
{
    TypeResolver = new DefaultTypeResolver(new TypeResolutionOptions { Strict = true }
        .RegisterType<Order>()
        .RegisterType<OrderItem>()),
};
```

Under `Strict`, only well-known aliases (`int`, `string`, `guid`…), a fixed set of structural core
types (`Func<>`, `Nullable<>`, `IQueryable<>`, `Enumerable`/`Queryable`…), registered types and
allow-listed assemblies/namespaces can be resolved — anything else throws `TypeResolutionException`.

Note that rebuilding an expression only *resolves* types and members; nothing executes until **you**
compile and invoke it.

## Performance

- Reflection is cached process-wide: type references, member lookups, resolved method signatures and
  per-type method tables are computed once per shape.
- Closure folding evaluates sub-trees with the expression **interpreter** (no JIT cost per serialization).
- Emitted anonymous types use compiled property accessors for `Equals`/`GetHashCode` (no per-call reflection).
- `ExpressionSerializer` is thread-safe — create it once and reuse it (or use `ExpressionSerializer.Default`).

## Extensibility

| Hook | Purpose |
|------|---------|
| `ITypeResolver` | Custom type naming/resolution (contract mappings, hardened policies). |
| `TypeResolutionOptions` | Aliases, known types, strict mode, assembly/namespace allow-lists. |
| `QueryRootProvider` | Re-binds serialized query roots to local `IQueryable` sources. |
| `ExtensionMethodTypes` | Static classes probed for instance-style extension calls in inferred payloads (defaults: `Queryable`, `Enumerable`). |
| `ConfigureJson` | Last-chance customization of the underlying `JsonSerializerOptions`. |
| `EnablePartialEvaluation` | Disable closure folding for hand-built trees that must stay verbatim. |

## Coverage

The test suite asserts a full support matrix over every `ExpressionType` value (round-trip with
structural equality via `ExpressionEqualityComparer`, JSON idempotence and compiled execution
equivalence on both sides):

- **Supported (83)** — all binaries (arithmetic, checked, bitwise, shifts, comparisons, coalesce,
  power, array index, all compound assignments), all unaries (negate, not, conversions, `TypeAs`,
  `Unbox`, `Throw`, increment/decrement, `IsTrue`/`IsFalse`, `OnesComplement`, `Quote`, `ArrayLength`),
  calls, members, indexers, lambdas, invocations, object/array/collection initializers (including
  nested member and list bindings), anonymous types, type tests (`TypeIs`/`TypeEqual`), defaults,
  blocks, loops, switch, try/catch/finally/fault, labels, gotos, runtime variables and debug info.
- **Supported via `Reduce()` (1)** — custom `Extension` nodes that are reducible.
- **Not supported (1)** — `Dynamic`: dynamic call sites carry runtime binders (`CallSiteBinder`)
  that have no portable representation. A clear `ExpressionSerializationException` explains this.

Other documented limitations: delegate-valued constants (no portable representation — keep logic as
expression trees), pointer types, and constants whose values System.Text.Json cannot serialize.

# eQuantic.Linq.Web

REST-friendly query syntax over the expression engine. The parser produces lean expression models
(names + raw values only); typing, member resolution and overload binding all come from the
engine's root-anchored inference.

```bash
dotnet add package eQuantic.Linq.Web
```

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
| Collections | `items:any(price:gt(50),category:eq(Tech))`, `items:all(…)`, `items:any()` |
| Aggregates | `items.count():gt(1)`, `items.count(price:gt(100)):gte(1)`, `items.sum(price):gt(200)`, `min`/`max`/`average` |
| Navigation | `customer.address.city:eq(Lisboa)` |
| Method segments | `notes.toLower():ct(gift)`, `customer.name.substring(0,3):eq(Ali)`, `customer.name.length:gte(5)` |
| Quoted values | `notes:ct('Leave at')`, `name:eq('it''s')` |

Values are plain text — the engine coerces them against the member's type (`decimal`, `DateTime`,
`Guid`, enums by name, booleans, nullables). Member and method names match case-insensitively, so
camelCase URLs work naturally.

Ordering: `orderBy=total:desc,customer.name` (direction defaults to `asc`; paths and method
segments allowed). Paging: `skip`/`take`. Projection: `select=id,customer.name,items.count()` —
optionally aliased (`select=name=customer.name`) — materialized through the engine's emitted
anonymous types.

Every parsed filter is exposed as an `ExpressionModel<T>` (`EntityQuery<T>.FilterModel`,
`QueryFilter.ParseModel<T>`), so a query received on a web endpoint can be re-serialized to JSON
and forwarded to another service unchanged.

LINQ-to-objects semantics are preserved verbatim — guard nullable navigations explicitly
(`and(notes:neq(null),notes.toLower():ct(gift))`) just as you would in C#.

## DTO → entity casting

REST APIs expose DTOs; queries run on entities. Consumers filter by the shape they know — the
`ExpressionCast` rewrites those expressions onto the data model, including computed mappings:

```csharp
using eQuantic.Linq.Expressions.Casting;

var cast = ExpressionCast.Create<OrderDto, Order>(options => options
    .Map(d => d.CustomerName, e => e.Customer.Name)                       // flattened navigation
    .Map(d => d.Revenue, e => e.Items.Sum(i => i.Price * i.Quantity))     // arithmetic aggregate
    .Map(d => d.Display, e => e.Customer.Name + " #" + e.Id)              // concatenation
    .Map(d => d.StatusName, e => e.Status.ToString())                     // type-shape conversion
    .Nested<ItemDto, OrderItem>(n => n.Map(i => i.Cost, e => e.Price)));  // collection elements

// GET /orders?filter=revenue:gt(200),items:any(cost:gt(50))&orderBy=revenue:desc
var query = EntityQuery.Parse<OrderDto>(Request.QueryString.Value)
    .Cast(cast)                       // rewrites filter, sorts and projection onto Order
    .Apply(dbContext.Orders);

// or predicate-level:
Expression<Func<Order, bool>> where = cast.Predicate(dtoPredicate);
```

- **Auto map by name** (case-insensitive) covers identical members; only differences need `Map`.
- **Column fallback**: members marked with `[Column("…")]` match by that name too — on the entity
  side (query-string paths resolve `[Column]` names) and on the DTO side (`CastOptions.ColumnFallback`).
- **Nested shapes**: collection/navigation element pairs re-bind generic LINQ calls
  (`Any<ItemDto>` → `Any<OrderItem>`) by unification; anonymous projections are re-emitted when
  argument types change.
- Unmapped or type-incompatible members fail with an `ExpressionCastException` telling you exactly
  which `Map(...)` to add. Value types must be preserved by maps (express conversions inside the
  target expression, e.g. `e => e.Status.ToString()`).
- `cast.Model(model)` casts serialized `ExpressionModel<TDto>` payloads directly — querystring →
  JSON → entity, end to end.

# eQuantic.Linq.Specification

The classic composable specification pattern (operator overloads, `And`/`AndAlso`/`Or`/`OrElse`/`Not`
with parameter rebinding — provider-friendly), rebuilt on the v3 engine with two new entry points:

```csharp
using eQuantic.Linq.Specification;
using eQuantic.Linq.Web.Specification;

// from a serialized expression payload (ExpressionModel<T> or raw JSON)
var fromWire = new ExpressionModelSpecification<Order>(json);

// from a REST filter query string (eQuantic.Linq.Web.Specification)
var fromClient = new QueryStringSpecification<Order>("status:in(Paid|Shipped),items.sum(price):gt(100)");

// compose with domain rules and execute
var visible = new DirectSpecification<Order>(o => !o.Deleted);
var results = orders.Where((fromClient & visible).SatisfiedBy());
```

Both expose their parsed filter as a serializable `ExpressionModel<T>` (`spec.Model`) for onward
transport.

## License

MIT © eQuantic Systems
