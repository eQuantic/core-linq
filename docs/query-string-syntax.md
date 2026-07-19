# Query-string syntax reference

Everything below parses into a typed expression tree through the engine's inference — the parser
itself knows no types, only names and raw values. Names match case-insensitively (plus `[Column]`
fallback), values coerce against the member's type.

```csharp
var query = EntityQuery.Parse<Order>(Request.QueryString.Value);
var page  = query.Apply(db.Orders);              // filter + orderBy + skip/take
var shaped = query.ApplyWithSelection(db.Orders); // + select projection
```

## Filters (`filter=`)

### Comparisons

| URL | Meaning |
|-----|---------|
| `total:gt(100)` | `o.Total > 100m` |
| `total:gte(100)` / `lt` / `lte` | `>=`, `<`, `<=` |
| `status:eq(Paid)` | `o.Status == OrderStatus.Paid` (enum by name) |
| `status:neq(Cancelled)` | `!=` |
| `id:3` | shorthand equality |
| `customer.isVip:true` | booleans coerce too |
| `createdAt:gte(2026-02-01)` | dates in ISO/invariant format |
| `reference:eq(aaaaaaaa-…)` | GUIDs |
| `discount:eq(null)` / `deliveredAt:neq(null)` | null tests over nullables |

### Strings

| URL | Meaning |
|-----|---------|
| `customer.name:ct(ali)` | `Contains` |
| `customer.name:nct(ali)` | `!Contains` |
| `customer.name:sw(A)` / `ew(a)` | `StartsWith` / `EndsWith` |
| `notes:ct('Leave at')` | quote values containing `, ( ) \|`; escape `'` as `''` |

### Logical composition

| URL | Meaning |
|-----|---------|
| `total:gt(100),status:eq(Paid)` | top-level comma = AND |
| `and(…)`, `or(…)`, `not(…)` | explicit groups, nest freely |
| `or(status:eq(Paid),and(total:gt(300),customer.isVip:true))` | mixed nesting |

Composition builds *balanced* trees, so very wide filters stay within depth limits.

### Membership

| URL | Meaning |
|-----|---------|
| `status:in(Paid\|Shipped)` | `values.Contains(o.Status)` — translates to SQL `IN` |
| `id:nin(1\|2\|3)` | negated membership |

### Collections

| URL | Meaning |
|-----|---------|
| `items:any(price:gt(50))` | `o.Items.Any(i => i.Price > 50m)` |
| `items:any(price:gt(50),category:eq(Tech))` | inner comma = AND over the element |
| `items:any(or(…))` | full composition inside |
| `items:all(quantity:gte(1))` | `All` |
| `items:any()` | non-empty |

### Aggregates & method segments

Paths may contain method segments; aggregates take a nested path or filters:

| URL | Meaning |
|-----|---------|
| `items.count():gt(1)` | `o.Items.Count() > 1` |
| `items.count(price:gt(100)):gte(1)` | predicate count |
| `items.sum(price):gt(200)` | `Sum(i => i.Price)` — also `min`, `max`, `average`/`avg` |
| `notes.toLower():ct(gift)` | instance methods by name |
| `customer.name.substring(0,3):eq(Ali)` | literal arguments |
| `customer.name.length:gte(5)` | properties of results |

## Sorting (`orderBy=`)

```
orderBy=total:desc,customer.name          direction defaults to asc
orderBy=customer.name.toLower():desc      method segments allowed
```

## Paging (`skip=` / `take=`)

Non-negative integers; applied after filtering and ordering.

## Projection (`select=`)

```
select=id,customer.name,items.count()     → { Id, CustomerName, ItemsCount }
select=name=customer.name                 → alias: { name = … }
```

Materializes through runtime-emitted anonymous types with structural equality — safe as `GroupBy`
keys and serializable in responses.

## Semantics worth knowing

- **Null safety is automatic where it matters.** With `NullGuardMode.Auto` (default), predicates
  applied to pure LINQ-to-objects (`.AsQueryable()` sources) get C# `?.`-style guards
  (`customer.address.city:eq(X)` won't throw on a null `Address`); relational providers receive the
  clean tree — SQL is null-safe natively.
- **Values parse with the invariant culture** by default (`300.5`, not `300,5` — the comma reads as
  a thousands separator). Configure `ExpressionSerializerOptions.FormatProvider` to change that.
- Repeated `filter=` parameters AND together; parsed filters are cached per options instance.
- Every parsed filter is exposed as a serializable `ExpressionModel<T>`
  (`EntityQuery<T>.FilterModel`, `QueryFilter.ParseModel<T>`) for onward transport.

## Building query strings in code

The grammar above is authored from typed member selectors — no magic strings — and round-trips:
`ToString()` produces the query string, and `Parse` reads one back into an inspectable, mutable
builder.

```csharp
using eQuantic.Linq.Web;

// Filters fold left to right: Where/And/Or chain flat; And/Or with a lambda nest a group.
string filter = QueryFilterBuilder.For<Order>()
    .Where(o => o.Total, FilterOperator.GreaterThan, 100m)
    .And(o => o.Customer.Name, FilterOperator.Contains, "li")
    .And(g => g.Where(o => o.Status, FilterOperator.Equal, OrderStatus.Paid)
               .Or(o => o.Customer.IsVip, FilterOperator.Equal, true))
    .ToString();
// "total:gt(100),customer.name:ct(li),or(status:eq(Paid),customer.isVip:eq(true))"

// A pure AND chain flattens to a comma list; a pure OR chain to one or(...) group:
QueryFilterBuilder.For<Order>()
    .Where(o => o.Status, FilterOperator.Equal, OrderStatus.Paid)
    .Or(o => o.Status, FilterOperator.Equal, OrderStatus.Shipped)
    .ToString();                              // "or(status:eq(Paid),status:eq(Shipped))"

// String paths work everywhere a selector does (for dynamic property names):
QueryFilterBuilder.For<Order>().Where("total", FilterOperator.GreaterThan, 100m);

// Sorts: typed → query string
string orderBy = QuerySortBuilder.For<Order>()
    .ByDescending(o => o.Total)
    .ThenBy(o => o.Customer.Name)
    .ToString();                              // "total:desc,customer.name"

// Reverse: query string → typed, then inspect / extend / re-emit
var predicate = QueryFilterBuilder.Parse<Order>("total:gt(100)")
    .And(o => o.Status, FilterOperator.Equal, OrderStatus.Paid)
    .ToPredicate();                           // straight to the engine
```

Clauses fold **left to right** — `Where(a).And(b).Or(c)` is `(a AND b) OR c` — and consecutive
same-operator clauses flatten, so a pure AND chain emits a comma list. Use the `And`/`Or`/`Not`
overloads that take a lambda group for explicit nesting. The builder emits exactly what the parser
accepts (values formatted invariantly, quoted only when the grammar requires it), so it's the
natural way to build the `filterBy`/`orderBy` a client sends, or to express filters in code and hand
`ToPredicate()`/`ToSorts()` to a repository. The reverse covers comparisons, null tests, `in`/`nin`
and `and`/`or`/`not`; constructs that aren't typed-buildable (collection quantifiers, aggregates,
method segments) are still parsed for execution by `QueryFilter.Parse`.

Next: [DTO → entity casting →](dto-casting.md)
