# eQuantic.Linq.Specification

The classic **specification pattern**, rebuilt on the
[eQuantic.Linq.Expressions](https://www.nuget.org/packages/eQuantic.Linq.Expressions) engine:
composable, provider-friendly predicates with operator overloads.

```csharp
using eQuantic.Linq.Specification;

var paid    = new DirectSpecification<Order>(o => o.Status == OrderStatus.Paid);
var big     = new DirectSpecification<Order>(o => o.Total > 100m);
var recent  = new DirectSpecification<Order>(o => o.CreatedAt > cutoff);

var spec = (paid & big) | !recent;          // operator overloads
var alt  = paid.AndAlso(big).OrNot(recent); // fluent equivalents

var results = orders.Where(spec.SatisfiedBy());
```

- `DirectSpecification`, `TrueSpecification`, `And`/`AndAlso`, `Or`/`OrElse`, `Not` — parameters are
  rebound (no `Invoke`), so composed expressions stay translatable by LINQ providers such as EF.
- Composition is powered by the engine's `PredicateExtensions`
  (`eQuantic.Linq.Expressions`), which offers the same `AndAlso`/`OrElse`/`Not` directly over
  `Expression<Func<T, bool>>` when you don't need the pattern.

## Specifications from serialized payloads

`ExpressionModelSpecification<T>` turns a serialized expression — an `ExpressionModel<T>` or its raw
JSON, received from another service, a queue or an API client — into a composable specification:

```csharp
// payload received over the wire (lean, hand-writable format)
var spec = new ExpressionModelSpecification<Order>(json);

var query = orders.Where((spec & onlyActive).SatisfiedBy());
```

Everything the engine offers applies: root-anchored type inference, strict type-resolution policies
for untrusted payloads (`ExpressionSerializer.CreateSecure`) and DTO→entity casting via a
customized `ExpressionSerializer`. The parsed filter stays available as `spec.Model` for onward
transport.

## Learn more

[ASP.NET Core & Specifications guide](https://github.com/eQuantic/core-linq/blob/main/docs/aspnetcore-and-specifications.md) ·
[all guides](https://github.com/eQuantic/core-linq/tree/main/docs)

MIT © eQuantic Tech
