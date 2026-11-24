# Module 5: Understanding the ASP.NET Core Request Pipeline

## What is middleware?
Every request travels through a **pipeline**: a chain of small components called **middleware**, set up in `Program.cs`
with `app.Use...`. Each one can:
1. do something with the request on the way **in**,
2. call `next` to pass it to the next middleware, **or not** (short-circuit: answer right away),
3. do something on the way **out**, after `next` returns.

```
request ──► Timer ──► Header ──► StaticFiles ──► BoxOffice ──► Routing ──► endpoint (controller / minimal API)
response ◄── Timer ◄── Header ◄──────────────────────────────────────────────────────┘
```

Like the backstage corridor of a theatre: each door can check you, send you back, or let you through,
and you pass the same doors again (in reverse) on the way out.

## The built-in pipeline
`UseStaticFiles` (serves `wwwroot` files and short-circuits), `UseRouting` (works out which endpoint matches),
`UseAuthentication` / `UseAuthorization` (who are you, are you allowed), `UseExceptionHandler` (catches errors from
everything after it), and finally the endpoints (`MapControllerRoute`, `MapGet`).

## Why order matters
Middleware runs **in the order it's added**. A middleware only sees requests that the ones *before* it let through,
and only catches errors from the ones *after* it. So:
- the request timer goes **first**, so it times everything;
- the exception handler goes **early**, so it catches errors from everything after it;
- the box office goes **after** `UseStaticFiles`, so the CSS of the "closed" page still loads;
- `/ping` branches off **before** the box office, so monitoring still works when it's closed.

## Writing custom middleware
**Inline**, for something tiny:
```csharp
app.Use(async (context, next) =>
{
    // on the way in
    await next(context);
    // on the way out
});
```

**As a class**, for anything bigger (convention-based middleware):
```csharp
public class RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context) { ... await next(context); ... }
}
app.UseMiddleware<RequestTimingMiddleware>();
```

**DI in middleware:** a middleware class is created **once**, when the app starts, so it is effectively a singleton.
- Constructor: singletons only (`ILogger`, `IOptions`, `IOptionsMonitor`).
- Scoped services (`IOptionsSnapshot`, a `DbContext`): add them as extra **`InvokeAsync` parameters**, resolved per request.

Same captive-dependency rule as Module 4.

## Conditional middleware and branching
| Method | What it does |
|---|---|
| `app.Map("/ping", branch => ...)` | requests starting with `/ping` leave the main pipeline and go down the branch only |
| `app.MapWhen(condition, branch => ...)` | the same, with any condition |
| `app.UseWhen(condition, branch => ...)` | runs extra middleware for some requests, then **rejoins** the main pipeline |
| `branch.Run(...)` | a terminal middleware: always answers, never calls `next` |

## Exception handling
`UseExceptionHandler("/Home/Error")` catches unhandled exceptions from everything after it and re-runs the request on
the error page. In Development the **developer exception page** is used instead (detailed stack trace), which is
why the template only adds `UseExceptionHandler` when `!IsDevelopment()`. For APIs, `AddProblemDetails()` gives
standard JSON error responses.

## Routing and endpoint execution
`UseRouting` *selects* the endpoint (it reads the URL and picks a controller action or `MapGet` lambda);
the endpoint *runs* at the end of the pipeline. Middleware placed between them can see which endpoint was chosen
(`context.GetEndpoint()`), which is how authorization knows about `[Authorize]`.

## Middleware vs. filters
**Filters** run *inside* MVC, around a controller action, after routing and model binding:

| | Middleware | Filter |
|---|---|---|
| Runs for | every request (static files, minimal APIs, MVC...) | MVC actions only |
| Knows about | the HTTP request: URL, headers | the action: its name, its arguments (`id = 2`), its result |
| Typical use | timing, headers, maintenance mode, exception handling, CORS | auditing, validation checks, per-action caching |

**Choosing:** if it's about HTTP in general, use middleware. If it needs to know about MVC actions or their arguments, use a filter.

## Steps
| Step | Course clip | What you do |
|---|---|---|
| 1 | What Is Middleware? / Exploring the Built-in Pipeline | An inline `app.Use` that adds an `X-Theatre` header |
| 2 | Writing Custom Middleware | `RequestTimingMiddleware`: logs how long each request took |
| 3 | Why Order Matters | `BoxOfficeMiddleware`: answers 503 when `BoxOfficeClosed` is on |
| 4 | Conditional Middleware and Pipeline Branching | `app.Map("/ping", ...)`, which answers even when closed |
| 5 | Middleware vs. Filters | `AuditFilter`: logs each MVC action with its arguments |

Check your work: `dotnet test --filter Module05`
