# Module 1: Getting Started with Dependency Injection

## The problem
`HomeController` creates its own `RoomCatalogue`:

```csharp
var catalogue = new RoomCatalogue();
```

The controller is welded to that one class. To use a different catalogue (from a database, or a fake one
in a test) you'd have to edit the controller.

## The idea: ask, don't create
A class lists what it needs in its **constructor**, and something else hands those things in.
In ASP.NET Core that "something else" is the **DI container**. You tell it once, in `Program.cs`,
which class to use for which need (**registering a service**). Then, whenever it creates a controller,
it reads the constructor and passes the matching objects in.

This is called **inversion of control**: the controller no longer controls how its dependencies are made.

## Coding to interfaces
The controller should depend on what it needs (an `IRoomCatalogue`: "something with `GetAll()`"),
not on one specific class (`RoomCatalogue`). Then any class that implements the interface can be plugged in.

## Framework services
ASP.NET Core registers many services for you, for example `ILogger<T>`, `IConfiguration` and
`IWebHostEnvironment`. You can ask for them in a constructor without registering anything.

## Why bother?
- **Swappable**: change one line in `Program.cs` to use a different implementation everywhere.
- **Testable**: a test can pass a fake catalogue straight into the controller's constructor.
- **Clear**: the constructor lists everything a class depends on.

## Steps
| Step | Course clip | What you do |
|---|---|---|
| 1 | Coding to Interfaces | Create `IRoomCatalogue`; make `RoomCatalogue` implement it |
| 2 | Inverting Control with Constructor Injection | `HomeController` asks for `IRoomCatalogue` in its constructor |
| 3 | Registering a Service | Register `IRoomCatalogue` → `RoomCatalogue` in `Program.cs` |
| 4 | Injecting Framework Dependencies | Also inject `ILogger<HomeController>`; log the room count |
| 5 | Advantages of Dependency Injection | Write your own unit test with a fake catalogue |

Check your work: `dotnet test --filter Module01`
