# eQuantic.Linq.Web.AspNetCore

ASP.NET Core binding for [eQuantic.Linq.Web](https://www.nuget.org/packages/eQuantic.Linq.Web):
receive fully parsed entity queries straight from the request query string — in MVC controllers and
in Minimal APIs.

## Setup

```csharp
builder.Services.AddEntityQueryBinding();          // optionally: options => { … }
```

## Minimal APIs

```csharp
app.MapGet("/orders", (EntityQueryModel<Order> query, AppDb db) =>
    query.Apply(db.Orders));

// GET /orders?filter=status:in(Paid|Shipped),items.sum(price):gt(100)&orderBy=total:desc&take=10
```

Invalid syntax returns HTTP 400 with the parse error and its position.

## MVC controllers

```csharp
[ApiController]
[Route("orders")]
public class OrdersController : ControllerBase
{
    [HttpGet]
    public IActionResult Get(EntityQuery<Order> query) =>
        Ok(query.Apply(db.Orders));
}
```

Parse errors land in `ModelState`, so `[ApiController]` produces the standard 400 problem-details
response automatically.

## DTO → entity casting

Bind by the DTO shape your API exposes, run on entities:

```csharp
app.MapGet("/orders", (EntityQueryModel<OrderDto> query, AppDb db) =>
    query.Query.Cast(OrderCasts.ToEntity).Apply(db.Orders));
```

## Whole queries as JSON bodies

POST-style search endpoints can bind the serializable envelope directly — filter, sorts, paging and
projection in one document:

```csharp
app.MapPost("/orders/search", (QueryModel<Order> body, AppDb db) =>
    Results.Ok(body.ToEntityQuery(options).Apply(db.Orders).ToList()));
```

## Extras

- `Request.ParseEntityQuery<T>()` — binding-free parsing wherever you have an `HttpRequest`.
- `AddEntityQueryBinding` also prepares MVC and minimal-API JSON options so serialized
  `ExpressionModel<T>` payloads (including lean hand-written ones) bind from request bodies —
  string enums, out-of-order `$type` discriminators and named floating-point literals included.
- Harden untrusted input with
  `AddEntityQueryBinding(o => o.UseStrictSerializer(typeof(Order), …))`.

## Learn more

[ASP.NET Core & Specifications guide](https://github.com/eQuantic/core-linq/blob/main/docs/aspnetcore-and-specifications.md) ·
[query-string syntax](https://github.com/eQuantic/core-linq/blob/main/docs/query-string-syntax.md) ·
[security](https://github.com/eQuantic/core-linq/blob/main/docs/security.md) ·
[all guides](https://github.com/eQuantic/core-linq/tree/main/docs)

MIT © eQuantic Tech
