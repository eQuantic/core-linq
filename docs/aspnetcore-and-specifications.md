# ASP.NET Core & Specifications

## One registration

```csharp
builder.Services.AddEntityQueryBinding(options =>
{
    options.UseStrictSerializer(typeof(Order), typeof(Customer)); // recommended at the edge
});
```

This registers the MVC model binder, exposes the configured `QueryStringOptions` through DI (the
minimal-API path picks it up too), and prepares both JSON stacks to accept expression-model
payloads in request bodies (case-insensitive properties, out-of-order `$type`, string enums).

## Minimal APIs

`EntityQueryModel<T>` implements `BindAsync`, so it binds like any framework type:

```csharp
app.MapGet("/orders", (EntityQueryModel<Order> query, AppDbContext db) =>
    Results.Ok(query.Apply(db.Orders).ToList()));
```

```
GET /orders?filter=total:gt(100),status:eq(Paid)&orderBy=total:desc&skip=0&take=20
```

Malformed input (`filter=total:oops(1)`, unknown members, bad values) is answered with a
**400 and the parser's message** — it never reaches your handler. The parameter also advertises
that 400 in endpoint metadata, so it shows up in OpenAPI.

`ApplyWithSelection` applies the `select=` projection as well (returns a non-generic `IQueryable`
of emitted anonymous shapes, ready to serialize).

## MVC controllers

Same type, attribute-free:

```csharp
[HttpGet]
public IActionResult Get(EntityQueryModel<Order> query) =>
    Ok(query.Apply(db.Orders).ToList());
```

Parse failures land in `ModelState` (standard `ValidationProblem` 400). Prefer manual control?
`Request.ParseEntityQuery<Order>(options)` does the same parse anywhere you hold an `HttpRequest`.

## Documenting the query surface (OpenAPI)

Bound query parameters are opaque to API consumers by default — Swagger shows an
`EntityQueryModel<Order>` endpoint with no usable contract. Two packages fix that, one per
OpenAPI stack:

```csharp
// Swashbuckle (eQuantic.Linq.Web.Swashbuckle):
builder.Services.AddSwaggerGen(o => o.AddEntityQueryDocumentation());

// Built-in Microsoft.AspNetCore.OpenApi, net10 (eQuantic.Linq.Web.OpenApi):
builder.Services.AddOpenApi(o => o.AddEntityQueryDocumentation());
```

Each endpoint binding `EntityQuery<T>` / `EntityQueryModel<T>` then advertises the five
parameters with the full [filter grammar](query-string-syntax.md), **the entity's real member
paths** (camelCase, `customer.name`, collections with element members, enum values, `[Column]`
aliases), examples generated from the actual members, and the 400 parse-error response. Custom
key names (`FilterKey = "q"`) are honored — pass your `QueryStringOptions` (the OpenApi
transformer also picks up the instance registered by `AddEntityQueryBinding`).

## Accepting whole queries as JSON

For POST-style search endpoints, bind the serializable envelope directly:

```csharp
app.MapPost("/orders/search", (QueryModel<Order> body, AppDbContext db) =>
    Results.Ok(body.ToEntityQuery(options).Apply(db.Orders).ToList()));
```

`QueryModel<T>` is the same document `EntityQuery<T>.ToQueryModel()` produces — so a client (or an
upstream service) can send filter + sorts + paging + projection as one JSON payload, hand-written
or round-tripped. `AddEntityQueryBinding` already configured the JSON options it needs.

## Specifications

`eQuantic.Linq.Specification` is the classic composable pattern, rebuilt on v3 — the boolean
composition (`&&`-style combination without `Invoke`) is now done by the engine's
`PredicateExtensions`, so composed specifications stay fully EF-translatable.

```csharp
public sealed class VipOrders : Specification<Order>
{
    public override Expression<Func<Order, bool>> SatisfiedBy() => o => o.Customer.IsVip;
}

public sealed class BigOrders : Specification<Order>
{
    public override Expression<Func<Order, bool>> SatisfiedBy() => o => o.Total > 500m;
}

var spec = new VipOrders().AndAlso(new BigOrders()).Not();
var results = db.Orders.Where(spec.SatisfiedBy());

// The concrete Specification<T> type also overloads & | ! (with true/false, so && works too):
var same = !(new VipOrders() & new BigOrders());
```

Two specifications connect the pattern to the serialization world:

```csharp
// A serialized filter (JSON model) as a specification — from a queue, a config store, another service:
var fromJson = new ExpressionModelSpecification<Order>(json);

// A raw client filter string as a specification (eQuantic.Linq.Web.Specification):
var fromQuery = new QueryStringSpecification<Order>("total:gt(100),status:eq(Paid)");

// They compose with your domain rules like any other spec:
var effective = fromQuery.AndAlso(new VipOrders());
```

Both expose `.Model` (`ExpressionModel<TEntity>`), so a specification received at the edge can be
re-serialized and forwarded — the pattern and the transport story are the same object.

This is the pattern for **"domain rules meet client filters"**: your invariants are specifications
under test, the client's filter is one more specification, and composition is guaranteed
translatable.

Next: [Helper extensions →](extensions.md)
