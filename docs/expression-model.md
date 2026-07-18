# The expression model & inference

## Two formats, one engine

The serializer speaks two dialects:

- **Full fidelity** (`ToNode` / `ToJson`): every node carries complete type and member references.
  Payloads are self-contained — nothing external is needed to rebuild them. Use this for
  service-to-service transport of arbitrary expressions.
- **Root-anchored / lean** (`ToModel<TRoot>` / `ExpressionModel<TRoot>`): the payload is anchored on
  a root entity and omits everything that can be *re-inferred* from it. Use this for API contracts
  and hand-written payloads.

```csharp
var model = ExpressionModel<Order>.From(o => o.Total > 100m);
string json = model.ToJson();
```

```json
{
  "parameters": [{ "id": 0, "name": "o" }],
  "body": {
    "$type": "binary", "nodeType": "GreaterThan",
    "left":  { "$type": "member", "member": { "name": "Total" }, "expression": { "$type": "parameter", "id": 0, "name": "o" } },
    "right": { "$type": "constant", "value": 100 }
  }
}
```

```csharp
Expression<Func<Order, bool>> predicate = ExpressionModel<Order>.FromJson(json).ToPredicate();
```

## How inference thinks

Decoding walks the tree top-down carrying an **expected type**, and fills gaps as it goes:

1. **Parameters.** The first parameter defaults to `TRoot`. References resolve by id, then by name
   in lexical scope, and a bare `{ "$type": "parameter" }` means "the root".
2. **Members.** `{ "member": { "name": "total" } }` — the owner comes from the target expression's
   static type. Matching is exact-case first, then case-insensitive (camelCase URLs just work),
   then by `[Column("…")]` attribute (column fallback).
3. **Constants.** A constant with no declared type is materialized against its *context*: the other
   operand of a comparison, the bound method parameter, the assigned member, the array element
   type. `"Paid"` becomes an `OrderStatus`, `"2026-01-01"` a `DateTime`, `100` a `decimal` —
   whatever the context demands (nullable-aware).
4. **Method calls.** The interesting one. Given
   ```json
   { "$type": "call", "method": { "name": "any" },
     "object": { "$type": "member", "member": { "name": "items" }, "expression": { "$type": "parameter" } },
     "arguments": [{ "$type": "lambda", "parameters": [{ "name": "i" }], "body": … }] }
   ```
   the binder finds no instance `Any` on `List<OrderItem>`, probes the configured extension holders
   (`Queryable` first, then `Enumerable` — the target's type picks the right one automatically),
   and **unifies** `Any<TSource>(IEnumerable<TSource>, Func<TSource, bool>)` against the decoded
   arguments: `TSource = OrderItem`, so `i` is typed `OrderItem` and its body decodes against that.
   Overload choice is deterministic across machines (stable candidate ordering).

The encoder plays the exact inverse game: in `TypeInfoMode.Minimal` it omits precisely what the
decoder can provably recover — both sides share the same rulebook, so lean JSON is idempotent.

## Hand-writing payloads

Everything above means a front-end can author filters with zero .NET type knowledge:

```json
{ "body": { "$type": "call", "method": { "name": "contains" },
  "object": { "$type": "member", "member": { "name": "name" },
              "expression": { "$type": "member", "member": { "name": "customer" }, "expression": { "$type": "parameter" } } },
  "arguments": [{ "$type": "constant", "value": "li" }] } }
```

→ `o => o.Customer.Name.Contains("li")`. Values may be strings regardless of the member type
(`"true"`, `"500"`, `"Paid"`, `"2026-01-01"` all coerce), `$type` may appear anywhere in the
object, and numbers-as-strings are accepted.

Need self-contained payloads instead? `serializer.ToModel<Order, bool>(filter, TypeInfoMode.Full)`.

## The whole query as one document

`ExpressionModel<T>` carries one lambda. To ship a *complete* query (filter + ordering + paging +
projection), use the envelope from `eQuantic.Linq.Web`:

```csharp
QueryModel<Order> document = entityQuery.ToQueryModel();     // serializable POCO
var revived = document.ToEntityQuery(options).Apply(source); // anywhere else
```

Next: [Query-string syntax reference →](query-string-syntax.md)
