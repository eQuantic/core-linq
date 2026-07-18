# eQuantic.Linq.Web.OpenApi

OpenAPI documentation for the [eQuantic.Linq](https://github.com/eQuantic/core-linq) query-string
surface, for the built-in `Microsoft.AspNetCore.OpenApi` stack. Without it, an endpoint binding
`EntityQueryModel<T>` produces no parameter documentation; with it, consumers see the actual
contract:

```csharp
builder.Services.AddOpenApi(options =>
{
    options.AddEntityQueryDocumentation();   // QueryStringOptions resolved from DI (AddEntityQueryBinding) or defaults
});
```

Every endpoint binding `EntityQuery<T>` (MVC) or `EntityQueryModel<T>` (Minimal APIs) then
documents:

- **`filter`** — the complete syntax cheat sheet (comparisons, `and`/`or`/`not`, `in`/`nin`,
  `any`/`all`, aggregates, method segments, quoting) plus the **entity's real member paths**
  discovered by reflection: camelCase, one navigation level (`customer.name`), collections with
  their element members (`items` → `price`, `quantity`…), enum values listed, `[Column]` aliases
  surfaced. Marked repeatable (values AND together).
- **`orderBy`**, **`select`** — syntax plus the same path catalog.
- **`skip`**, **`take`** — non-negative integers.
- Examples generated from the entity's actual members (`id:gt(0),notes:ct(a)`).
- The **400** parse-error response.

Key names follow your `QueryStringOptions`: pass an instance explicitly, or let the transformer
resolve the one registered by `AddEntityQueryBinding`.

Targets net10.0 (`IOpenApiOperationTransformer`). Using Swashbuckle instead? Use
[eQuantic.Linq.Web.Swashbuckle](https://www.nuget.org/packages/eQuantic.Linq.Web.Swashbuckle).

## Learn more

[ASP.NET Core & Specifications guide](https://github.com/eQuantic/core-linq/blob/main/docs/aspnetcore-and-specifications.md) ·
[query-string syntax reference](https://github.com/eQuantic/core-linq/blob/main/docs/query-string-syntax.md) ·
[all guides](https://github.com/eQuantic/core-linq/tree/main/docs)

MIT © eQuantic Tech
