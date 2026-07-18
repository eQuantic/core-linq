# Getting started

## The problem

A LINQ expression is the richest way .NET has to describe *intent over data*:

```csharp
Expression<Func<Order, bool>> filter = o => o.Total > 100m && o.Items.Any(i => i.Price > 50m);
```

But an expression tree is a runtime object graph full of `MethodInfo`s, `ParameterExpression`s and
compiler-generated closures — it cannot leave the process. The moment you need to **send a filter to
another service**, **store it**, **audit it**, or **receive it from a front-end**, you need a
portable representation.

That is what this library does, in both directions and with full fidelity:

```
Expression tree  ⇄  ExpressionNode model (plain objects)  ⇄  JSON
```

## Your first round-trip

```csharp
using eQuantic.Linq.Expressions;

var minimum = 100m; // captured variables are folded automatically

Expression<Func<Order, bool>> filter = o => o.Total > minimum && o.Items.Any(i => i.Price > 50m);

string json = ExpressionSerializer.Default.ToJson(filter);
// … transport, store, audit …
var rebuilt = ExpressionSerializer.Default.FromJson<Func<Order, bool>>(json);

var results = orders.Where(rebuilt.Compile());   // in memory
var query   = dbOrders.Where(rebuilt);           // or through EF — the tree translates
```

Three things happened silently:

1. **Closure folding.** `minimum` lives in a compiler-generated closure class that doesn't exist
   anywhere else; it was evaluated and embedded as the constant `100`. Structure that carries
   meaning (object creation, lambdas, whole `Queryable` pipelines) is *never* folded.
2. **Portable references.** Types serialize as short aliases (`decimal`, `int`) or
   namespace-qualified names with no runtime-specific assembly identities — a payload produced on
   .NET 10 reads fine on .NET Framework via `netstandard2.0`.
3. **Exact reconstruction.** The rebuilt tree is structurally identical to the original — same
   operators, same members, same generic method instantiations. 84 of the 85 `ExpressionType`
   values round-trip (the exception, `Dynamic`, is impossible by design and fails with a clear
   error).

## The mental model

The intermediate model is just objects — inspect it, build it, transform it:

```csharp
ExpressionNode node = ExpressionSerializer.Default.ToNode(filter);
// LambdaNode { Parameters = [o], Body = BinaryNode { NodeType = AndAlso, Left = …, Right = … } }
```

Every node family of `System.Linq.Expressions` has a serializable twin: `BinaryNode`, `UnaryNode`,
`MethodCallNode`, `MemberNode`, `LambdaNode`, `ConstantNode`, … down to blocks, loops and
try/catch. In JSON each node carries a `$type` discriminator:

```json
{ "$type": "binary", "nodeType": "GreaterThan",
  "left":  { "$type": "member", "member": { "name": "Total" }, "expression": { "$type": "parameter" } },
  "right": { "$type": "constant", "value": 100 } }
```

Notice what is *missing*: no type names anywhere. That's the second big idea —
[root-anchored inference](expression-model.md). When a payload is anchored on a root entity
(`ExpressionModel<Order>`), everything below is inferred: parameter types, member owners, constant
types, generic method arguments. Payloads become lean enough for a front-end to write by hand.

## Which package do I need?

| You want to… | Use |
|--------------|-----|
| Serialize/rebuild expressions, cast DTO→entity, compose predicates | `eQuantic.Linq.Expressions` |
| Accept `?filter=total:gt(100)&orderBy=total:desc` style URLs | `eQuantic.Linq.Web` |
| Receive `EntityQuery<T>` directly in controllers / minimal APIs | `eQuantic.Linq.Web.AspNetCore` |
| Model domain rules as composable specifications | `eQuantic.Linq.Specification` |
| Turn client filter strings into specifications | `eQuantic.Linq.Web.Specification` |

## One rule to remember

**Nothing executes at deserialization time.** Rebuilding a payload only *resolves* types and
members; code runs when **you** compile or hand the tree to a provider. Still, on untrusted input
you should constrain what can be resolved — that's a one-liner, and it's covered in the
[security guide](security.md).

Next: [The expression model & inference →](expression-model.md)
