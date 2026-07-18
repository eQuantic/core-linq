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

## Extras

- `Request.ParseEntityQuery<T>()` — binding-free parsing wherever you have an `HttpRequest`.
- `AddEntityQueryBinding` also prepares MVC and minimal-API JSON options so serialized
  `ExpressionModel<T>` payloads (including lean hand-written ones) bind from request bodies —
  string enums, out-of-order `$type` discriminators and named floating-point literals included.
- Harden untrusted input by configuring a strict-mode `ExpressionSerializer` in
  `QueryStringOptions`.

Full documentation: <https://github.com/eQuantic/core-linq>

MIT © eQuantic Systems
