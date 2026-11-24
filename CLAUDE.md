# Training project: DramaSchoolHub (DI & middleware)

Learning project for the Pluralsight course "Applying Dependency Injection & Middleware in ASP.NET Core 10"
(Steve Gordon; demo app "Tennis Booking"). The user watches a module, then applies it here; Claude is the tutor.
Sibling projects in the same style: `c:\Projects\Training\AspNetCore`, `EfCore`, `Linq`, `eShopEnhanced`.

## How modules work
- One app that evolves (not separate lesson files): `src/DramaSchoolHub` (MVC + later minimal APIs, port 5090).
  Spots to change are marked `// MODULE N, step M`. Theory goes in `docs/module-NN-*.md`; README holds the curriculum.
- Tests: `tests/DramaSchoolHub.Tests/ModuleNNTests.cs`, names `ModuleNN_StepM_...` (two digits). Tests find the user's
  new types by name via reflection so they compile before the types exist; failure messages say what to add.
- Before handing out a module: verify a reference solution in a scratchpad copy and check the stubs fail helpfully.
  Never leave solutions in the user's files. Build modules one at a time, when the user asks.
- Course → project names: tennis court → rehearsal room, court booking → room booking, `WeatherForecaster`-style
  examples → drama-school equivalents. Course may use Visual Studio; the user uses VS Code and `dotnet` CLI.

## How to teach (user preferences)
- Explain the concept briefly, then ONE step at a time as fill-in-the-blank code (`____`) with the expected result.
  The user says "ready" (or "ρεαδυ" on the Greek layout); then read the file, run that step's tests, give feedback.
- Always show the sample data (e.g. the rooms table) next to a question that uses it.
- "Solve it for me" = do it in their file, then explain each part.
- If code looks stale, the user may not have saved (Auto Save is on in `.vscode/settings.json`).
- Recurring slips: forgetting `async`, list name instead of the lambda parameter, missing `;` or `)`,
  lowercase property names, copying course names into this project, messy indentation after pasting.
