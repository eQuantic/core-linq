# eQuantic.Linq

The eQuantic.Linq engine in one reference. This is a **meta-package**: it has no code of its own,
it just brings in the framework-free core of the family so you can start with a single dependency.

```bash
dotnet add package eQuantic.Linq
```

You get:

| Package | What it gives you |
|---------|-------------------|
| [eQuantic.Linq.Expressions](https://www.nuget.org/packages/eQuantic.Linq.Expressions) | Serialize any expression tree to JSON and rebuild it; root-anchored inference; DTO→entity casting; predicate composition. |
| [eQuantic.Linq.Web](https://www.nuget.org/packages/eQuantic.Linq.Web) | Query-string syntax (`filter`, `orderBy`, `skip`, `take`, `select`) parsed into typed expressions, plus code-first typed query builders. |
| [eQuantic.Linq.Specification](https://www.nuget.org/packages/eQuantic.Linq.Specification) | The composable specification pattern on the v3 engine, including specifications from serialized payloads. |

All three target `netstandard2.0`, `net8.0` and `net10.0` and pull **no framework dependencies** —
usable in libraries, console apps and workers alike.

## When to reference a single package instead

Most apps only need part of the engine — reference the specific package and keep your graph lean:

- Just serializing/rebuilding expressions → `eQuantic.Linq.Expressions`.
- A web API accepting query strings → `eQuantic.Linq.Web` (+ the ASP.NET Core / Swagger / OpenAPI
  integration packages, which are **not** part of this meta-package by design).
- Domain rules as specifications → `eQuantic.Linq.Specification`.

Full documentation: <https://github.com/eQuantic/core-linq/tree/main/docs>

MIT © eQuantic Tech
