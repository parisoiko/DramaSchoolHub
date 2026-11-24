# DramaSchoolHub

ASP.NET Core 10 MVC + minimal APIs. A rehearsal-room booking site for a drama school that showcases dependency injection
and the middleware pipeline. Public portfolio project: keep the code clean and professional. No database: rooms and
bookings live in memory on purpose.

## Layout
- `src/DramaSchoolHub/`: the web app (Controllers, Extensions, Filters, Middleware, Models, Notifications, Services, Views, wwwroot)
- `tests/DramaSchoolHub.Tests/`: xUnit unit tests and `WebApplicationFactory<Program>` integration tests, grouped by feature
- `docs/`: `NOTES.md` (concept explanations, kept out of the code) and README screenshots
- `.github/workflows/ci.yml`: builds and tests on every push to `main`

## Commands (from the repo root)
- Run: `dotnet run --project src/DramaSchoolHub` (http://localhost:5090), or F5 in VS Code
- Test: `dotnet test` (37 tests)
- If the developer's debugger (F5) is running, `bin/` is locked: build or test with a separate output folder (`dotnet test -o <tmp>`).

## Conventions
- Registrations live in `Extensions/ServiceCollectionExtensions.cs` (one method per area); `Program.cs` stays a short table of contents.
- New code uses primary constructors for injection, like the existing controllers and services.
- Comments explain *why*, not basic concepts (those belong in `docs/NOTES.md`).
- Integration tests override settings with `factory.WithSettings(...)` and capture logs with `factory.WithLogs(...)` (`TestSupport.cs`).
- Package versions: Microsoft.AspNetCore.* 10.0.13 (the installed ASP.NET Core runtime).

## Gotchas
- Pipeline order in `Program.cs` is part of the design: timer first, box office after `UseStaticFiles`, `/ping` before the box office.
- Middleware classes are effectively singletons: scoped services go into `InvokeAsync` parameters, never the constructor.
- `BookingStore` is a shared singleton: every member must take `_lock`, reads included.
- `MaxBookingHours` is limited to 1–5 by `[Range]`, and options are validated at start-up, so a bad `appsettings.json` value stops the app.
- `HomeController.Room` is attribute-routed (`/rooms/{id}`), so `/Home/Room/2` no longer exists.
