# Module 4: Avoiding Dependency Injection Pitfalls

## 1. Captive dependencies and scope validation
A service must not depend on one that lives **shorter** than itself:

| A... | may depend on... |
|---|---|
| Singleton | Singleton only |
| Scoped | Scoped or Singleton |
| Transient | anything |

If a singleton takes a scoped service in its constructor, it's created once and keeps that one scoped object
forever. "One per request" silently becomes "one for everybody", and with a `DbContext` that means users sharing
a database connection. The scoped object is **captive**.

**Scope validation** catches this. In the Development environment, ASP.NET Core checks every registration when
the app is built (`ValidateScopes` and `ValidateOnBuild`), and refuses to start:

```
Cannot consume scoped service 'ScopedPass' from singleton 'Usher'.
```

In Production this check is off (it costs start-up time), so the bug would go unnoticed. That's why you develop in Development.

## 2. Thread safety with singleton services
A singleton is shared by **every request at the same time**, and requests run on different threads, truly in parallel.
`List<T>` and `_nextId++` are not safe for that:
- two `Add`s at once can lose a booking or crash the list ("Destination array was not long enough");
- `_nextId++` is really *read, add 1, write*: two threads can read the same value and give two bookings the same Id;
- `ToList()` while another thread adds can crash ("Collection was modified").

A `lock` lets one thread at a time into a block of code; the others wait at the door:

```csharp
private readonly Lock _lock = new();   // .NET 9+: a dedicated lock type

lock (_lock)
{
    // only one thread at a time in here
}
```

Every method that touches the shared data, **reads included**, must take the same lock.
Alternatives: thread-safe collections like `ConcurrentDictionary<TKey, TValue>`, or `Interlocked.Increment(ref _nextId)` for counters.

## 3. Options: validation and lifetimes
`Configure<T>` accepts anything: a typo or a `0` only shows up when a user hits the page. Validate instead:

```csharp
builder.Services.AddOptions<BookingRules>()
    .Bind(builder.Configuration.GetSection("BookingRules"))
    .ValidateDataAnnotations()                       // [Range], [Required], ... on the class
    .Validate(rules => rules.OpeningHour < rules.ClosingHour, "OpeningHour must be before ClosingHour.")
    .ValidateOnStart();                              // check now, not at first use
```

With `ValidateOnStart` a bad setting stops the app at start-up, with a message naming the setting. Fail fast.

The three options interfaces have different lifetimes:

| Interface | Lifetime | Reads `appsettings.json`... |
|---|---|---|
| `IOptions<T>` | Singleton | once |
| `IOptionsSnapshot<T>` | Scoped | at the start of every request (picks up edits without a restart) |
| `IOptionsMonitor<T>` | Singleton | whenever the file changes; `.CurrentValue` is always current |

`IOptionsSnapshot` is scoped, so a singleton can't use it (captive dependency again): singletons use `IOptionsMonitor`.

## 4. Disposal of services
Some objects hold resources (a database connection, a file) and implement `IDisposable` to release them.
The container **disposes what it created**:
- scoped services: when the request (scope) ends;
- transient services: when the scope that created them ends;
- singletons: when the app shuts down.

Pitfalls:
- An object you create yourself and register (`AddSingleton(new Thing())`) is **not** disposed by the container: you made it, you clean it up.
- Disposable **transients** resolved from the root container (outside any request) are kept until the app stops: a memory leak.
- Never call `Dispose()` yourself on something the container gave you; others may still be using it.

## 5. Using DI in hosted services
A **hosted service** (usually a `BackgroundService`) runs alongside the web app, outside any request:
cleaning up, sending queued emails, refreshing caches. It's registered with `AddHostedService<T>()` and is a **singleton**.

So it can't take scoped services in its constructor (captive dependency again). And since there's no request, there's no
scope either. Instead it takes an `IServiceScopeFactory` and **creates its own scope** for each piece of work:

```csharp
using IServiceScope scope = scopeFactory.CreateScope();
var store = scope.ServiceProvider.GetRequiredService<IBookingStore>();
```

The scope is disposed at the end of the `using`, along with every scoped service it created.
Your eShopEnhanced Blazor search component does the same, for the same reason.

## Steps
| Step | Course clip | What you do |
|---|---|---|
| 1 | Captive Dependencies and Scope Validation | An `Usher` that takes a `ScopedPass`; register it as Singleton, read the error, fix it |
| 2 | Thread Safety with Singleton Services | Make `BookingStore` thread-safe with `lock`; register it as a singleton |
| 3 | Options Pattern: Lifetimes and Validation | Validate `BookingRules` at start-up; the Rules page uses `IOptionsSnapshot` |
| 4 | Disposal of Services | `Usher` implements `IDisposable` |
| 5 | Using DI in Hosted Services | `BookingCleanupService` removes ended bookings in its own scope |

Check your work: `dotnet test --filter Module04`
