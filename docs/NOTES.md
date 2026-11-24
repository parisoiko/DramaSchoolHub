# Study notes

Notes I wrote while following the Pluralsight course **"Applying Dependency Injection & Middleware in ASP.NET Core 10"**
(Steve Gordon). The course builds a tennis-court booking app; I applied each module to my own drama-school theme instead.
Paths are relative to `src/DramaSchoolHub/`.

## Contents

1. [Getting started with dependency injection](#1-getting-started-with-dependency-injection)
2. [Service lifetimes and configuration](#2-service-lifetimes-and-configuration)
3. [Injecting services](#3-injecting-services)
4. [Avoiding DI pitfalls](#4-avoiding-di-pitfalls)
5. [The request pipeline](#5-the-request-pipeline)
6. [Advanced patterns](#6-advanced-patterns)

---

## 1. Getting started with dependency injection

**Ask, don't create.** A class lists what it needs in its constructor, and the **DI container** hands those things in.
`HomeController` doesn't write `new RoomCatalogue()`; it asks for an `IRoomCatalogue`. This is **inversion of control**:
the controller no longer decides how its dependencies are made.

**Coding to interfaces.** Depending on `IRoomCatalogue` ("something with `GetAll()`") instead of the concrete class
means any implementation can be plugged in: a database one later, or a fake one in a test.

**Registering.** `Program.cs` tells the container once which class to use for which need:
`services.AddSingleton<IRoomCatalogue, RoomCatalogue>()`. Framework services such as `ILogger<T>`, `IConfiguration`
and `IWebHostEnvironment` are registered already.

**Why bother:** one line swaps an implementation everywhere; tests can pass fakes straight into constructors
(`tests/.../HomeControllerTests.cs`); and the constructor lists everything a class depends on.

**Adding a method to an interface** breaks every class that implements it, fakes included. That's the interface
doing its job: all implementations must keep the same promise.

## 2. Service lifetimes and configuration

| Lifetime | Registered with | A new object is created... | Good for |
|---|---|---|---|
| Transient | `AddTransient` | every time one is asked for | small stateless helpers |
| Scoped | `AddScoped` | once per scope = once per HTTP request | things tied to one request, e.g. a `DbContext` |
| Singleton | `AddSingleton` | once for the whole app, shared by everyone | fixed data, settings, caches |

- **Scoped means one per type per request.** Fifty services asking for the same scoped type in one request all get
  the same object; two different types are two objects; the next request starts with an empty "box".
- **A singleton is not per user.** It's created when first needed after the server starts and is shared by every
  user's requests until the app stops. Per-user state belongs in cookies, sessions or the database.
- **Variables vs objects:** `scoped1` and `scoped2` are two references to one object, which is why their codes match.

**In this project:** `Services/StagePass.cs` gives every pass a random code; `LifetimesController` asks for each kind
twice and `/Lifetimes` shows which codes match. The `Usher` is a second scoped service: its greeting shows the same
code as the controller's scoped pass.

**The Options pattern** turns a section of `appsettings.json` into a typed class:
1. a plain class whose property names match the JSON (`Models/BookingRules.cs`);
2. bind it once (`Extensions/ServiceCollectionExtensions.cs`, `AddBookingRules`);
3. ask for `IOptions<BookingRules>` and use `.Value`.

Without the binding, `IOptions<T>` still resolves, but every value is the default (0). That's a quiet bug,
which is why the rules are validated at start-up (section 4).

## 3. Injecting services

| Way | Looks like | Use when | Here |
|---|---|---|---|
| Constructor | `HomeController(IRoomCatalogue catalogue, ...)` | the default | every controller |
| Action | `Room(int id, [FromServices] IOptions<BookingRules> rules)` | only one action needs it | `HomeController.Room` |
| Minimal API | `(IRoomCatalogue catalogue) => ...` | endpoints in `Program.cs` | `/api/...` |
| Razor view | `@inject IOptions<BookingRules> Rules` | a view with no model of its own needs data | the footer in `_Layout.cshtml` |
| Service locator | `GetRequiredService<T>()` | almost never: it hides dependencies | only the hosted service, to create its own scope |

- MVC parameters come from the request (route, query, form) unless marked `[FromServices]`. Minimal APIs check the
  container themselves, so no attribute is needed there.
- **Primary constructors** (C# 12) such as `class RulesController(IOptionsSnapshot<BookingRules> options)` do the same
  as a constructor plus private fields, with less code.

## 4. Avoiding DI pitfalls

**Captive dependencies.** A service must not depend on one that lives shorter than itself:

| A... | may depend on... |
|---|---|
| Singleton | singletons only |
| Scoped | scoped or singleton |
| Transient | anything |

A singleton `Usher` holding a scoped `ScopedPass` would keep the first request's pass forever. In Development the
container validates this when the app starts and refuses to run: *"Cannot consume scoped service 'ScopedPass' from
singleton 'Usher'"*. Production skips that check, so the bug would ship silently.

**Thread safety.** A singleton is used by many requests at the same moment. `List<T>.Add`, `_nextId++` and even
`ToList()` break when two threads touch the list at once: lost items, duplicate ids, crashes. `Services/BookingStore.cs`
takes one `lock` in every method, **reads included**, and keeps the locked code short (it returns a copy). The test
runs 20,000 parallel adds and reads. Data that never changes, like the rooms, needs no lock.

**Options: validation and lifetimes.**
```csharp
services.AddOptions<BookingRules>()
    .Bind(configuration.GetSection("BookingRules"))
    .ValidateDataAnnotations()   // [Range] on the class
    .Validate(rules => rules.OpeningHour < rules.ClosingHour, "OpeningHour must be before ClosingHour.")
    .ValidateOnStart();          // fail fast, naming the bad setting
```

| Interface | Lifetime | Picks up edits to appsettings.json | Used in |
|---|---|---|---|
| `IOptions<T>` | singleton | no, read once | the footer, `HomeController.Room` |
| `IOptionsSnapshot<T>` | scoped | yes, every request | `RulesController`, `BoxOfficeMiddleware` |
| `IOptionsMonitor<T>` | singleton | yes, via `CurrentValue` | for singletons that need live settings |

**Disposal.** The container disposes what it created: scoped and transient services when their scope ends, singletons
when the app stops. `Usher` implements `IDisposable` and logs when the request ends. Objects you create yourself
(`AddSingleton(new Thing())`) are yours to dispose; never dispose something the container gave you.

**Hosted services.** `Services/BookingCleanupService.cs` is a `BackgroundService`: a singleton that runs alongside the
app, outside any request. It cleans up once at start-up and then every minute with a `PeriodicTimer`, and creates its own
scope for each round (`IServiceScopeFactory.CreateScope()`), which is required as soon as it uses a scoped service such
as a `DbContext`. `stoppingToken` ends the loop cleanly when the app shuts down.

## 5. The request pipeline

Every request passes through **middleware** in the order it was added. Each one can act on the way in, call `next`
or answer straight away (short-circuit), and act again on the way out:

```
request ──► Timer ──► X-Theatre ──► StaticFiles ──► BoxOffice ──► Routing ──► MVC / minimal APIs
                                         │
                                         └── /ping ──► "pong"
```

| Piece | File | Why it's there |
|---|---|---|
| Request timer | `Middleware/RequestTimingMiddleware.cs` | first, so it times everything after it |
| `X-Theatre` header | `Program.cs` (`app.Use`) | set before `next`: headers can't change once the response has started |
| `/ping` branch | `Program.cs` (`app.Map`) | branches off before the box office, so health checks always answer |
| Box office | `Middleware/BoxOfficeMiddleware.cs` | after static files, so the "closed" page still gets its CSS |

- **Inline vs class middleware:** `app.Use(async (context, next) => ...)` for a few lines; a class with a
  `RequestDelegate` constructor parameter and `InvokeAsync(HttpContext)` for anything bigger.
- **DI in middleware:** middleware classes are created once, so the constructor may only take singletons. Scoped
  services (`IOptionsSnapshot`) go in as extra `InvokeAsync` parameters, resolved per request.
- **`app.Map` vs `MapGet`:** `app.Map` branches at the point where it's written; `MapGet` endpoints run at the end of
  the pipeline, after the box office.
- **Exception handling:** `UseExceptionHandler` catches errors from everything after it; Development shows the
  developer exception page instead.
- **Routing:** `UseRouting` picks the endpoint; the endpoint runs at the end. Middleware in between can see which one
  was picked, which is how authorization knows about `[Authorize]`.

**Middleware vs filters.** Filters run inside MVC, around an action, after model binding. `Filters/AuditFilter.cs` logs
*"Running HomeController.Room with id=2"*: middleware only sees the URL `/rooms/2`. Filters don't run for minimal API
endpoints or static files. Rule of thumb: HTTP for every request → middleware; anything about MVC actions → filter.

## 6. Advanced patterns

- **Service descriptors:** each `Add...` call adds a descriptor (service type, implementation, lifetime) to
  `builder.Services`, which is just a list.
- **Several implementations of one interface:** `EmailSender` and `SmsSender` both implement `INotificationSender`.
  Asking for one gives the **last** registered; asking for `IEnumerable<INotificationSender>` gives all of them, in
  registration order (`/api/notifications/preview`). `TryAdd...` only registers if nothing is there yet.
- **Extension methods:** `Extensions/ServiceCollectionExtensions.cs` groups registrations (`AddDramaSchoolServices`,
  `AddBookingRules`, `AddNotificationSenders`), so `Program.cs` reads like a table of contents. `this` on the first
  parameter makes it an extension method; returning `services` allows chaining.
- **Keyed services (.NET 8+):** `AddKeyedSingleton<INotificationSender, SmsSender>("urgent")` plus
  `[FromKeyedServices("urgent")]` picks one specific implementation. Keyed registrations aren't included when you ask
  without a key.
- **Third-party containers:** Scrutor adds assembly scanning and decorators on top of the built-in container; Autofac
  replaces it (`UseServiceProviderFactory`) for modules and property injection. Worth it only when you hit a real limit.
