# Helper extensions

A small, deliberate set in `eQuantic.Linq.Expressions` — each one exists because building the same
thing by hand is either verbose or subtly wrong. (v3 intentionally did **not** port the old
extension surface; this is the curated replacement.)

## Composing predicates — `PredicateExtensions`

The naive way to combine two lambdas is `Expression.Invoke`, and most providers reject it. These
helpers **rebind parameters and merge bodies**, producing plain trees any provider translates:

```csharp
using eQuantic.Linq.Expressions;

Expression<Func<Order, bool>> big = o => o.Total > 100m;
Expression<Func<Order, bool>> paid = o => o.Status == OrderStatus.Paid;

var both   = big.AndAlso(paid);   // o => o.Total > 100m && o.Status == Paid
var either = big.OrElse(paid);    // short-circuiting ||
var not    = big.Not();           // o => !(o.Total > 100m)
// And / Or are the non-short-circuiting & and | variants.
```

The classic dynamic-search pattern, with `PredicateBuilder` seeds:

```csharp
var filter = PredicateBuilder.True<Order>();          // neutral for AND chains

if (criteria.MinTotal is { } min)  filter = filter.AndAlso(o => o.Total > min);
if (criteria.Status is { } st)     filter = filter.AndAlso(o => o.Status == st);
if (criteria.VipOnly)              filter = filter.AndAlso(o => o.Customer.IsVip);

var page = db.Orders.Where(filter);
// PredicateBuilder.False<T>() is the seed for OR chains ("match any of…").
```

For non-boolean shapes there's the general form:
`first.Compose(second, Expression.Add)` merges any two same-shaped lambdas with the operator you
choose.

`predicate.WithNullGuards()` applies the C# `?.`-style rewrite explicitly — useful when you're
about to compile a deep-path predicate for LINQ-to-objects
(see [NullGuards in the internals guide](how-it-works.md#null-guards)).

## Member paths — `MemberPathExtensions`

The bridge between lambdas and the string world (query strings, sort maps, audit logs):

```csharp
// Lambda → string
((Expression<Func<Order, string>>)(o => o.Customer.Name)).GetMemberPath();      // "Customer.Name"
((Expression<Func<Order, int>>)(o => o.Id)).GetMemberName();                    // "Id"
selector.GetMemberPath(columnFallback: true);   // honors [Column("…")] names

// String → lambda (full inference: case-insensitive, [Column] fallback, nested)
var selector = MemberPathExtensions.ToSelector<Order>("customer.name");
// Expression<Func<Order, string>> — typed by what the path resolves to
```

`ToSelector` is how you accept `?orderBy=customer.name` in code that never referenced
`eQuantic.Linq.Web`.

## Models straight onto queries — `QueryableModelExtensions`

When a filter arrives as a serialized model, skip the ceremony:

```csharp
IQueryable<Order> q = db.Orders
    .Where(model)                       // ExpressionModel<Order> overload of Where
    .OrderByPath("customer.name")       // string path, inferred & typed
    .ThenByPath("total", descending: true);

db.Orders.WhereJson(json);              // same, one step earlier (raw JSON in)
```

Every overload takes an optional `ExpressionSerializer` — pass your
[`CreateSecure`](security.md) instance when the model came from outside.

## What deliberately didn't make the cut

Reimplementations of things LINQ already does, stringly-typed everything-helpers, and `Invoke`-based
composition. If you migrate from v2 and miss an extension, the replacement is usually one of the
above or a [query-string](query-string-syntax.md) / [specification](aspnetcore-and-specifications.md)
feature that solves the underlying need.

Next: [How it works →](how-it-works.md)
