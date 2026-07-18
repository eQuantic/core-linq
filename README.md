# eQuantic.Linq

**Serialize any .NET expression tree to JSON and rebuild it with full fidelity — then build query
strings, DTO casting, specifications and ASP.NET Core binding on top of it.**

```
Expression<Func<Order, bool>>  ⇄  ExpressionNode model  ⇄  JSON
```

```csharp
using eQuantic.Linq.Expressions;

Expression<Func<Order, bool>> filter = o => o.Total > minimum && o.Items.Any(i => i.Price > 50m);

string json = ExpressionSerializer.Default.ToJson(filter);       // captured `minimum` folded in
var rebuilt = ExpressionSerializer.Default.FromJson<Func<Order, bool>>(json);

var results = orders.Where(rebuilt.Compile());                   // in memory
var query   = dbContext.Orders.Where(rebuilt);                   // or translated by EF
```

And the same engine, one layer up:

```csharp
// GET /orders?filter=total:gt(100),items:any(price:gt(50))&orderBy=total:desc&take=10
app.MapGet("/orders", (EntityQueryModel<Order> query, AppDbContext db) =>
    Results.Ok(query.Apply(db.Orders).ToList()));
```

## Why

Expression trees are the richest way .NET describes intent over data — but they can't leave the
process. This library makes them **portable**: send filters between services, store and audit
queries, accept them from front-ends (query string or hand-written JSON), rewrite them across
DTO/entity boundaries, and rebuild them anywhere as *real, provider-translatable* trees.

- **Proven coverage** — 84 of 85 `ExpressionType` values round-trip (only `Dynamic` is impossible
  by design); all 63 `Queryable` operators, whole `IQueryable` pipelines with re-bindable roots,
  closures, anonymous types, blocks/loops/try-catch.
- **Root-anchored inference** — `ExpressionModel<TRoot>` payloads carry names and values only;
  parameter types, member owners, constant types and generic method bindings are inferred. Lean
  enough for a front-end to write by hand.
- **Nothing executes at deserialization** — decode resolves and assembles; execution is yours.
  A hardened mode (`ExpressionSerializer.CreateSecure`) locks resolution to your contract types.
- **Performance-minded** — process-wide reflection caches, emitted anonymous types with compiled
  accessors, cached parsed filters; serializers and casts are thread-safe singletons.
- Targets `netstandard2.0`, `net8.0`, `net10.0`.

## Packages

| Package | Purpose |
|---------|---------|
| **eQuantic.Linq.Expressions** | The engine: expression ⇄ model ⇄ JSON, root-anchored inference, DTO→entity casting, predicate composition, null guards. |
| **eQuantic.Linq.Web** | Query-string syntax (`filter`, `orderBy`, `skip`, `take`, `select`) parsed into typed expressions; `QueryModel<T>` whole-query documents. |
| **eQuantic.Linq.Web.AspNetCore** | `EntityQuery<T>` binding for Minimal APIs and MVC, parse errors as 400s, JSON options prepared for expression payloads. |
| **eQuantic.Linq.Specification** | The composable specification pattern on the v3 engine, plus `ExpressionModelSpecification` for serialized payloads. |
| **eQuantic.Linq.Web.Specification** | `QueryStringSpecification` — client filter strings as composable specifications. |

```bash
dotnet add package eQuantic.Linq.Expressions   # or the layer you need — each pulls what it builds on
```

## Documentation

The guides are written to be read in order — problem, concept, verified example, pitfalls:

1. [Getting started](docs/getting-started.md) — the mental model and your first round-trip.
2. [The expression model & inference](docs/expression-model.md) — how lean payloads work; hand-writing JSON filters.
3. [Query-string syntax reference](docs/query-string-syntax.md) — the complete grammar, operator by operator.
4. [DTO → entity casting](docs/dto-casting.md) — consumers filter by the DTO they know; queries run on entities.
5. [Security](docs/security.md) — the real threat model and the one-liner that handles it.
6. [ASP.NET Core & Specifications](docs/aspnetcore-and-specifications.md) — binding, JSON query documents, domain rules.
7. [Helper extensions](docs/extensions.md) — `PredicateBuilder`, member paths, models straight onto `IQueryable`.
8. [How it works](docs/how-it-works.md) — internals, limitations and FAQ.

Every code sample in the docs is backed by a test in this repository
(`test/eQuantic.Linq.Expressions.Tests`).

## A taste of each layer

**Hand-written payload → typed predicate** (inference resolves everything):

```json
{ "body": { "$type": "call", "method": { "name": "any" },
  "object": { "$type": "member", "member": { "name": "items" }, "expression": { "$type": "parameter" } },
  "arguments": [{ "$type": "lambda", "parameters": [{ "name": "i" }],
    "body": { "$type": "binary", "nodeType": "GreaterThan",
      "left": { "$type": "member", "member": { "name": "price" }, "expression": { "$type": "parameter", "name": "i" } },
      "right": { "$type": "constant", "value": 50 } } }] } }
```

```csharp
var predicate = ExpressionModel<Order>.FromJson(json).ToPredicate();
// o => o.Items.Any(i => i.Price > 50m) — i : OrderItem inferred, Enumerable.Any<OrderItem> bound
```

**Query strings, piecemeal:**

```csharp
var filtered = orders.WhereQueryString("status:in(Paid|Shipped),items.sum(price):gt(100)");
var sorted   = orders.OrderByQueryString("customer.name.toLower():asc,total:desc");
```

**DTO casting with computed mappings:**

```csharp
var cast = ExpressionCast.Create<OrderDto, Order>(o => o
    .Map(d => d.CustomerName, e => e.Customer.Name)
    .Map(d => d.Revenue, e => e.Items.Sum(i => i.Price * i.Quantity)));

var page = EntityQuery.Parse<OrderDto>(queryString).Cast(cast).Apply(db.Orders);
```

**Specifications, composed and serializable:**

```csharp
var fromClient = new QueryStringSpecification<Order>("total:gt(100),status:eq(Paid)");
var active     = new DirectSpecification<Order>(o => o.Status != OrderStatus.Cancelled);
var results    = db.Orders.Where((fromClient & active).SatisfiedBy());
```

## Repository layout

```
src/        the five packages (+ src/TypeScript, a query-string builder for front-ends)
test/       the proof: round-trip matrices, EF Core integration, hardening, payload files
benchmarks/ BenchmarkDotNet round-trip benchmarks
docs/       the educational guides
```

## License

MIT © eQuantic Tech — see [LICENSE](LICENSE).
