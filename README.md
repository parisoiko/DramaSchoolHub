# DramaSchoolHub

Rehearsal-room booking for a drama school, built module by module while following the Pluralsight course
**"Applying Dependency Injection & Middleware in ASP.NET Core 10"** (Steve Gordon).

The course builds a *Tennis Booking* app; this project applies the same concepts to our own theme:
tennis courts → rehearsal rooms, court bookings → room bookings.

| Folder | What it is |
|---|---|
| `docs/` | Theory notes, one file per module |
| `src/DramaSchoolHub` | The app (F5: "Run DramaSchoolHub", http://localhost:5090) |
| `tests/DramaSchoolHub.Tests` | The tests that check each step: `dotnet test --filter Module01` |

Run one step's tests: `dotnet test --filter Module01_Step3`

## Curriculum (course module → what we build)

1. **Getting Started with Dependency Injection**: `HomeController` stops creating `RoomCatalogue` with `new`;
   an `IRoomCatalogue` interface, constructor injection, registering the service, injecting `ILogger<T>`,
   and a unit test with a fake catalogue
2. **Service Lifetimes and Configuration**: a page that shows transient / scoped / singleton instances side by side;
   booking rules (opening hours, longest booking) from `appsettings.json` with the Options pattern
3. **Injecting Services**: action injection (`[FromServices]`), a minimal API (`/api/rooms`), `@inject` in a Razor view
4. **Avoiding DI Pitfalls**: a captive dependency and scope validation, a thread-safe singleton booking store,
   options validation, disposal, a background service that clears past bookings
5. **The Request Pipeline**: custom middleware (request timing, "box office closed" mode), ordering, branching,
   exception handling, middleware vs. filters
6. **Advanced Patterns**: several notification senders (email, SMS) behind one interface, registration extension methods,
   keyed services, third-party containers
