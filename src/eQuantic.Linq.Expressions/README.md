# eQuantic.Linq.Expressions

Convert **any .NET expression tree** into a fully structured, serialization-friendly model (JSON) — and rebuild the exact expression back, with full fidelity.

```
Expression<Func<Order, bool>>  ⇄  ExpressionNode model  ⇄  JSON
```

- **84 of 85 `ExpressionType` values** round-trip with structural fidelity (only `Dynamic` is impossible by design) — binaries, unaries, calls, members, initializers, anonymous types, blocks, loops, switch, try/catch, labels and gotos.
- **The complete `Queryable` surface** (all 63 operators on .NET 10, including `LeftJoin`, `CountBy`, `AggregateBy`, `Shuffle`) serialized as whole `IQueryable` pipelines — query root included, re-bindable on the other side.
- **Closures handled**: captured locals, `this` references and compiler display classes fold into portable constants before serialization.
- **Anonymous types travel**: projections are described structurally and re-materialized with matching equality semantics (safe for `GroupBy`/`Distinct` keys).
- Targets `netstandard2.0`, `net8.0` and `net10.0`.

## Quick start

```csharp
using eQuantic.Linq.Expressions;

Expression<Func<Order, bool>> filter = o => o.Total > minimum && o.Items.Any(i => i.Price > 50m);

string json = ExpressionSerializer.Default.ToJson(filter);
// …transport, store, audit…
var rebuilt = ExpressionSerializer.Default.FromJson<Func<Order, bool>>(json);
```

## The typed model — `ExpressionModel<TRoot>`

Anchor the payload on a root entity and everything below it is inferred — parameter types, member
owners, constant types, generic method arguments. Payloads become lean enough to write by hand:

```json
{
  "body": {
    "$type": "binary",
    "nodeType": "GreaterThan",
    "left":  { "$type": "member", "member": { "name": "total" }, "expression": { "$type": "parameter" } },
    "right": { "$type": "constant", "value": 100 }
  }
}
```

```csharp
Expression<Func<Order, bool>> predicate = ExpressionModel<Order>.FromJson(json).ToPredicate();
```

Member and method names match case-insensitively (including `[Column("…")]` fallback), enum
constants accept strings, and instance-style extension calls (`items.Any(…)`) are bound by generic
unification.

## DTO → entity casting

Rewrite expressions authored over the API shape onto the data model — with computed mappings:

```csharp
using eQuantic.Linq.Expressions.Casting;

var cast = ExpressionCast.Create<OrderDto, Order>(o => o
    .Map(d => d.CustomerName, e => e.Customer.Name)
    .Map(d => d.Revenue, e => e.Items.Sum(i => i.Price * i.Quantity))
    .Nested<ItemDto, OrderItem>(n => n.Map(i => i.Cost, e => e.Price)));

Expression<Func<Order, bool>> where = cast.Predicate(dtoPredicate);
```

## Composition & helpers

Provider-friendly predicate composition (parameters rebound — no `Invoke`), member-path bridging
and model-direct querying:

```csharp
var filter = PredicateBuilder.True<Order>()
    .AndAlso(o => o.Total > 100m)
    .AndAlso(o => o.Customer.IsVip);            // also Or/OrElse/Not/Compose

var path     = selector.GetMemberPath();                                // o => o.Customer.Name → "Customer.Name"
var back     = MemberPathExtensions.ToSelector<Order>("customer.name"); // string path → typed lambda

db.Orders.Where(model)                           // ExpressionModel<Order> straight onto IQueryable
         .OrderByPath("customer.name").ThenByPath("total", descending: true);
```

`NullGuards.Apply(predicate)` (or `predicate.WithNullGuards()`) rewrites deep member chains with
C# `?.`-style protection for LINQ-to-objects execution.

## Security & performance

- One-liner hardening for untrusted payloads:
  `ExpressionSerializer.CreateSecure(typeof(Order), …)` — strict type resolution locked to your
  contract types, a method allow-list, and tightened depth/node/anonymous-type caps. Fine-grained
  policies via `TypeResolutionOptions` and `MethodFilter`.
- Nothing executes at deserialization time — decoding only resolves types/members and assembles an
  inert tree.
- Process-wide reflection caches, interpreter-based closure folding, compiled anonymous-type
  accessors and deterministic overload binding keep hot paths reflection-free and reproducible.

## Learn more

Educational guides (concepts, hand-written payloads, internals, FAQ):
[getting started](https://github.com/eQuantic/core-linq/blob/main/docs/getting-started.md) ·
[the model & inference](https://github.com/eQuantic/core-linq/blob/main/docs/expression-model.md) ·
[DTO casting](https://github.com/eQuantic/core-linq/blob/main/docs/dto-casting.md) ·
[security](https://github.com/eQuantic/core-linq/blob/main/docs/security.md) ·
[extensions](https://github.com/eQuantic/core-linq/blob/main/docs/extensions.md) ·
[how it works](https://github.com/eQuantic/core-linq/blob/main/docs/how-it-works.md)

MIT © eQuantic Tech
