# eQuantic.Linq v3 — Documentation

Learn the library in the order it was designed to be learned: first the engine, then the layers on
top of it. Every code sample in these guides is backed by a test in the repository.

## Guides

| # | Guide | You will learn |
|---|-------|----------------|
| 1 | [Getting started](getting-started.md) | The mental model: expression ⇄ model ⇄ JSON, your first round-trip, when to use which package. |
| 2 | [The expression model & inference](expression-model.md) | How `ExpressionModel<TRoot>` anchors a payload on an entity and infers everything else — and how to hand-write payloads. |
| 3 | [Query-string syntax reference](query-string-syntax.md) | The complete filter grammar, sorting, paging and projections, operator by operator. |
| 4 | [DTO → entity casting](dto-casting.md) | The REST walkthrough: consumers filter by the DTO they know, queries run on your entities. |
| 5 | [Security](security.md) | The threat model of deserializing expressions and how to run safely on untrusted input. |
| 6 | [ASP.NET Core & Specifications](aspnetcore-and-specifications.md) | Binding `EntityQuery<T>` in Minimal APIs and MVC; composing domain rules with specifications. |
| 7 | [Helper extensions](extensions.md) | `PredicateBuilder`, member paths, applying models/JSON straight to `IQueryable`. |
| 8 | [How it works](how-it-works.md) | Internals: closure folding, the inference binder, emitted anonymous types, null guards, caches — plus limitations & FAQ. |

## Packages at a glance

```
eQuantic.Linq.Expressions          the engine: expression ⇄ JSON, inference, casting, extensions
    └── eQuantic.Linq.Web          query-string syntax → typed expressions
            ├── eQuantic.Linq.Web.AspNetCore      binding for MVC & Minimal APIs
            └── eQuantic.Linq.Web.Specification   query strings as specifications
    └── eQuantic.Linq.Specification               the composable specification pattern
```

Start with guide 1 — it takes ten minutes and everything else builds on it.

Maintainers: the automated release flow (semantic-release, commit types → versions, channels) is
described in [releasing.md](releasing.md).
