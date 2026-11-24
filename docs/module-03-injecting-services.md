# Module 3: Injecting Services

## Service resolution mechanisms
"Resolving" a service means the container handing you an object. There are several ways to ask:

| Way | Looks like | Use when |
|---|---|---|
| **Constructor injection** | `public HomeController(IRoomCatalogue catalogue)` | the default: the class needs it in most of its methods |
| **Action injection** | `public IActionResult Room(int id, [FromServices] IOptions<BookingRules> rules)` | only **one** action needs it |
| **Minimal API parameters** | `app.MapGet("/api/rooms", (IRoomCatalogue catalogue) => ...)` | endpoints written in `Program.cs` |
| **`@inject` in a Razor view** | `@inject IOptions<BookingRules> Rules` | a view (often the layout) needs data that isn't in its model |
| **Asking the container directly** | `HttpContext.RequestServices.GetRequiredService<T>()` | almost never: the "service locator" style hides what a class depends on |

All of them use the same registrations and the same lifetimes. Only the place where you ask differs.

## Constructor injection (recap)
You've done this since Module 1. The constructor lists everything the class depends on, which is honest and easy to test.
If a constructor grows very long, that's often a sign the class does too much.

## Action injection: `[FromServices]`
MVC fills action parameters from the request (route values, query string, form). `[FromServices]` says
"not from the request, from the DI container". Useful when only one action needs a service: the constructor
stays small, and the service isn't created for the other actions.

## Minimal APIs
A minimal API endpoint is a lambda mapped straight to a URL in `Program.cs`, with no controller.
Parameters are filled automatically: `id` from the route, and anything registered in the container
(like `IRoomCatalogue`) from DI. No `[FromServices]` needed: the framework checks the container itself.
Returning an object sends it as JSON; `Results.NotFound()` sends a 404.

## `@inject` in Razor views
`@inject <Type> <Name>` creates a property in the view filled from the container. It's handy for the layout,
which every page uses but which has no model of its own. Keep it for display data; real work belongs in controllers and services.

## Steps
| Step | Course clip | What you do |
|---|---|---|
| 1 | Constructor Injection | Add `GetById` to `IRoomCatalogue`, `RoomCatalogue` and your test's `FakeRoomCatalogue` |
| 2 | Action Injection | A `Room` action on `HomeController` that gets the booking rules with `[FromServices]` |
| 3 | Injecting Services into Minimal APIs | `/api/rooms` and `/api/rooms/{id}` in `Program.cs` |
| 4 | Injecting Services in Razor Views | A footer in `_Layout.cshtml` with the opening hours, via `@inject` |

Check your work: `dotnet test --filter Module03`
