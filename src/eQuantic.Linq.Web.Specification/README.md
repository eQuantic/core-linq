# eQuantic.Linq.Web.Specification

Turns REST **filter query strings** into composable specifications, bridging
[eQuantic.Linq.Web](https://www.nuget.org/packages/eQuantic.Linq.Web) and
[eQuantic.Linq.Specification](https://www.nuget.org/packages/eQuantic.Linq.Specification).

```csharp
using eQuantic.Linq.Specification;
using eQuantic.Linq.Web.Specification;

// GET /orders?filter=status:in(Paid|Shipped),items.sum(price):gt(100)
var fromClient = new QueryStringSpecification<Order>(filterFromQueryString);

// compose with domain rules before executing
var visible = new DirectSpecification<Order>(o => !o.Deleted);
var results = orders.Where((fromClient & visible).SatisfiedBy());
```

- The full filter grammar applies: comparisons, `and`/`or`/`not`, `any`/`all`, aggregates
  (`items.sum(price):gt(100)`), method segments, `in`/`nin`, null tests, quoted values.
- Parsing is eager — invalid syntax fails at construction with position information.
- The parsed filter stays available as a serializable `ExpressionModel<T>` (`spec.Model`) so it can
  be forwarded to another service unchanged.
- Harden untrusted input with `new QueryStringOptions().UseStrictSerializer(typeof(Order), …)`.

## Learn more

[ASP.NET Core & Specifications guide](https://github.com/eQuantic/core-linq/blob/main/docs/aspnetcore-and-specifications.md) ·
[query-string syntax reference](https://github.com/eQuantic/core-linq/blob/main/docs/query-string-syntax.md) ·
[all guides](https://github.com/eQuantic/core-linq/tree/main/docs)

MIT © eQuantic Tech
