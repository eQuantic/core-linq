# How it works

The internals, for when you need to predict behavior — or extend it.

## Encoding: partial evaluation

Before anything is serialized, the tree passes a funcletizer (`EnablePartialEvaluation`, on by
default). It finds maximal subtrees with **no dependency on lambda parameters** and evaluates them
into constants — this is what turns closure fields (`minimum`), `this.Threshold`, `DateTime.UtcNow`
and `config.Limit * 2` into portable values.

What it refuses to fold is the interesting part, because folding there would destroy meaning:

- **Construction:** `new`, member init, list init, array creation — kept as structure so the
  receiver rebuilds shapes (anonymous types included) instead of receiving opaque blobs.
- **Lambdas and quotes** — behavior is never collapsed to a value.
- **`Queryable` calls and `IQueryable`-typed subtrees** — otherwise `source.Where(...).Count()`
  inside your expression would *execute the query at serialization time* and embed the result.
  (Found and fixed by test: zero-lambda operators like `Distinct`/`Count` were the trap.)
- Statement nodes, assignments, and comparer-typed arguments (comparers aren't value-serializable).

## Encoding: query roots

An `IQueryable` pipeline bottoms out in a provider-specific constant (an EF `DbSet<Order>`, an
`EnumerableQuery<Order>`). That constant becomes a symbolic `QueryRootNode` carrying only the
element type. On decode, the root is re-bound: to whatever `QueryRootProvider` you configure, or to
an inert placeholder you re-target later. That's how a whole *query*, not just a lambda, moves
between processes — and why decode(encode(x)) is stable even though the concrete provider changed.

## Decoding: the inference binder

Decoding is expected-type-driven (details in [the model guide](expression-model.md)). The
mechanical core:

- **Member resolution** on the owner's static type: exact name → case-insensitive → `[Column]`
  fallback (attribute matched by full name string, so no EF package reference).
- **Method binding**: candidates from the target type, then the configured extension holders
  (`Queryable` before `Enumerable`; the receiver's static type decides which applies). Generic
  methods go through a **type unifier** that solves type parameters against the decoded argument
  types — that's how `any` on `List<OrderItem>` becomes `Enumerable.Any<OrderItem>` and the inner
  lambda's parameter gets its type.
- **Deterministic overload choice**: candidates are ordered by a stable rule (non-generic first,
  then arity, then a penalty for `char`-parameter overloads — `Contains(char)` must lose to
  `Contains(string)` or EF can't translate — then signature ordinal). Same payload, same tree, on
  every machine.
- **Constant materialization** happens against the context type, culture-invariant by default
  (`FormatProvider`), accepting strings for numbers/dates/enums/GUIDs so hand-written payloads
  survive JavaScript's type system.

Everything resolved is cached (`ReflectionCache`): member lookups, method candidate lists,
constructed generics. Reflection cost is paid once per shape, not per payload.

## Anonymous types

There is no `Anon1` class on the receiving side, so the factory **emits** one:
`Reflection.Emit`-generated types with init-only properties, a `[JsonConstructor]` constructor with
camelCase parameters, and structural `Equals`/`GetHashCode`/`ToString` via compiled accessors —
which is why an emitted type works as a `GroupBy` key and serializes cleanly in responses.
Emission is keyed by shape (names + types), cached, and capped (`MaxAnonymousTypes`, or disabled
entirely with `AllowAnonymousTypes = false` — [`CreateSecure`](security.md) tightens the cap).

## Null guards

`NullGuards.Apply(lambda)` rewrites member/call chains the way the C# `?.` operator would: each
nullable link is checked, results lift to `Nullable<T>`, and boolean predicates land back on
`bool` via `?? false`. It's a *rewrite*, not a runtime wrapper — the guarded tree is still a plain
expression.

The query-string layer applies this automatically under `NullGuardMode.Auto`: at `Apply` time it
looks at the `IQueryable`'s provider — an `EnumerableQuery` source (pure LINQ-to-objects) gets
guards; a real provider (EF, anything relational) gets the clean tree, because SQL semantics
handle nulls and the extra checks would just noise up the translation.

## Performance notes

- The serializer, options, casts and parsed-filter caches are **thread-safe; build once, reuse**.
- Query-string filters are cached per `QueryStringOptions` instance (`CacheParsedFilters`).
- Decode allocates the tree you asked for and touches reflection only on cache misses.
- `benchmarks/eQuantic.Linq.Benchmarks` (BenchmarkDotNet) tracks round-trip costs; run
  `dotnet run -c Release --project benchmarks/eQuantic.Linq.Benchmarks`.

## Limitations — the honest list

| Thing | Status |
|-------|--------|
| `ExpressionType.Dynamic` | **Impossible by design** — dynamic call sites carry runtime binders with no portable form. Clear exception at encode time. The other 84 node kinds round-trip (proven by the coverage matrix test). |
| `^1` / `..` literals | C# itself forbids index/range *literals* inside expression trees (CS8791/92); use `new Index(1, true)` / captured values — those round-trip fine. |
| Comparer arguments (`OrderBy(…, comparer)`) | The lambda structure round-trips; a custom `IComparer<T>` instance is not value-serializable — re-supply it on the receiving side. |
| Provider translation | Round-trip fidelity ≠ provider capability. EF still has its own rules (e.g. SQLite can't order by `decimal`); the serializer faithfully rebuilds trees the provider may still refuse. |
| Multi-dimensional array constants | Serializable via node structure (`NewArrayBounds`), but raw multi-dim constants don't fit JSON arrays — prefer jagged. |

## FAQ

**Is `Compile()` called anywhere during decode?** No. Decode only resolves and assembles. Compile
happens when you call it (or `ToPredicate()`, which is `FromModel` + nothing else).

**Can I inspect/rewrite a payload without .NET types?** Yes — the node model is plain objects and
the JSON is stable and documented; the TypeScript companion (`src/TypeScript`) builds payloads
directly.

**Why does my decoded constant have a different CLR type than the original?** Inference materializes
constants against *context*. `100` compared to a `decimal` member **is** `100m` — that's the
feature. In `TypeInfoMode.Full` payloads, declared types always win.

**Round-trip equality?** `ExpressionEqualityComparer` performs structural comparison (used by the
test-suite to prove `decode(encode(x)) ≡ x`, including expression-valued constants).

---

That's the tour. Back to the [documentation index](README.md).
