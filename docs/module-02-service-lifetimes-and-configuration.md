# Module 2: Service Lifetimes and Configuration

## The Microsoft DI container
`builder.Services` is a list of **registrations**: "when someone asks for X, give them Y, made like *this*".
`builder.Build()` turns that list into the container (the `IServiceProvider`). From then on the container
creates objects for you and decides **how long each one lives**.

## The three lifetimes
| Lifetime | Registered with | A new object is created... | Good for |
|---|---|---|---|
| **Transient** | `AddTransient<T>()` | every time someone asks | small, cheap, stateless helpers |
| **Scoped** | `AddScoped<T>()` | once per **scope** = once per HTTP request | things tied to one request (e.g. an EF Core `DbContext`) |
| **Singleton** | `AddSingleton<T>()` | once, for the whole life of the app | shared state, caches, expensive set-up |

In Module 1 you registered `RoomCatalogue` as a singleton: its rooms never change, so one shared object is enough.

**Rule of thumb:** a service must not depend on a service that lives *shorter* than itself
(a singleton holding a scoped object keeps it alive forever). That trap is called a
**captive dependency**, and Module 4 covers it.

## Seeing it: stage passes
Each `StagePass` gets a random 6-character `Code` when it is created, so the same code means the same object.
`LifetimesController` asks for each kind **twice**:

| | Within one request | Across two requests (refresh) |
|---|---|---|
| Transient | two different codes | changes |
| Scoped | the same code twice | changes |
| Singleton | the same code twice | never changes (until the app restarts) |

## Configuration and the Options pattern
Settings live in `appsettings.json` (and `appsettings.Development.json`, environment variables,
command-line arguments...). All of them are merged into one `IConfiguration`.

Reading `config["BookingRules:MaxBookingHours"]` everywhere works, but it gives you strings and typos fail silently.
The **Options pattern** is the better way:

1. A plain class whose property names match the JSON names (`BookingRules`).
2. Bind the section once in `Program.cs`:
   `builder.Services.Configure<BookingRules>(builder.Configuration.GetSection("BookingRules"));`
3. Ask for `IOptions<BookingRules>` wherever you need it and use `.Value`.

`IOptions<T>` reads the settings once (singleton). Its siblings `IOptionsSnapshot<T>` (re-read per request)
and `IOptionsMonitor<T>` (notified of changes), plus validating options, come in Module 4.

## Steps
| Step | Course clip | What you do |
|---|---|---|
| 1 | Service Lifetimes / Demo: Service Lifetime Project | Create `StagePass` and its three subclasses |
| 2 | Transient, Singleton and Scoped Services | Register each pass with a different lifetime |
| 3 | (see it) | `LifetimesController`: ask for each pass twice; open `/Lifetimes` and refresh |
| 4 | Configuration and the Options Pattern | A `BookingRules` section in `appsettings.json` and a matching class |
| 5 | Demo: Completing the Initial Registrations | `Configure<BookingRules>`, `IOptions<BookingRules>` in a `RulesController` |

Check your work: `dotnet test --filter Module02`
