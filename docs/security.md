# Security

## The threat model, honestly

Deserializing an expression **does not execute anything**. `FromJson` only resolves types/members
and assembles an inert tree; code runs when *you* call `Compile()` or hand the tree to a provider.
So the real risks on untrusted input are:

1. **Resolution reach.** A payload names types and methods by string. Unconstrained, an attacker
   can make the rebuilt tree call *any* resolvable static/instance method — harmless at
   deserialization, dangerous the moment you execute the tree.
2. **Resource abuse.** Absurdly deep/wide payloads (parser bombs), or unbounded anonymous-type
   emission filling the process with generated types.
3. **Data reach.** Even a "safe" tree is a query your provider will happily run — expressive
   filters over the wrong `IQueryable` are a data-exposure problem, not a serializer problem.

The library addresses 1 and 2 with an opt-in hardened mode; 3 is yours (expose pre-scoped queries:
tenant-filtered, column-limited).

## The one-liner

```csharp
var serializer = ExpressionSerializer.CreateSecure(typeof(Order), typeof(Customer), typeof(OrderItem));
```

This flips every lever at once:

| Lever | Default serializer | `CreateSecure` |
|-------|-------------------|----------------|
| Type resolution | any loadable type | **Strict**: aliases + safe BCL primitives + your contract types only |
| Method binding | any resolvable method | **allow-list**: `Queryable`/`Enumerable`/`string`/`Math`/date-time/`Guid`/collections/your contracts |
| `MaxDepth` / `MaxNodes` | 1024 / 100 000 | 256 / 10 000 |
| Anonymous types | up to 2048 emitted shapes | capped at 256 |

A payload referencing `System.IO.File`, `Process`, `Activator` — or any type you didn't register —
fails resolution with a clear exception before a tree ever exists. A method outside the allow-list
fails binding even if its declaring type is resolvable.

Both policies compose instead of being all-or-nothing:

```csharp
var serializer = ExpressionSerializer.CreateSecure(
    [typeof(Order)],
    resolution => resolution.AllowedNamespaces.Add("MyApp.Contracts"),   // widen resolution
    options => options.MethodFilter = m =>                               // tighten binding further
        m.DeclaringType == typeof(Queryable) || m.DeclaringType == typeof(string));
```

`MethodFilter` is the final gate — return `false` and the call node is rejected regardless of
anything else.

## Hardening the query-string layer

The parser inherits whatever serializer it's given, so the same one-liner exists there:

```csharp
var options = new QueryStringOptions().UseStrictSerializer(typeof(Order), typeof(Customer));
var query = EntityQuery.Parse<Order>(queryString, options);
```

The grammar itself is already narrow (an attacker can't invent arbitrary calls from
`filter=…` — only member paths, whitelisted operators and simple method segments), plus a nesting
cap of 64 on `and`/`or`/`not` groups. Strict mode simply removes the residual resolution surface.

## Practical checklist

- [ ] Untrusted JSON payloads → `CreateSecure` with exactly your contract types.
- [ ] Untrusted query strings → `QueryStringOptions.UseStrictSerializer(...)`.
- [ ] Execute rebuilt trees only against **pre-scoped** `IQueryable`s (tenant filter applied first).
- [ ] Catch `QueryStringParseException` / serializer exceptions at the boundary and return 400 —
      they carry precise, safe-to-log messages.
- [ ] Cap paging yourself (`take` is client-controlled; clamp it server-side).
- [ ] Leave `EnablePartialEvaluation` alone on the *decode* side — it only affects encoding; nothing
      evaluates during decode by design.

Trusted internal service-to-service transport? `ExpressionSerializer.Default` is fine — strict mode
costs you flexibility you may legitimately need there.

Next: [ASP.NET Core & Specifications →](aspnetcore-and-specifications.md)
