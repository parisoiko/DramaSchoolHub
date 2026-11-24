# Module 6: Advanced Patterns

## Service descriptors
Every `Add...` call adds a **service descriptor** to `builder.Services`: *service type* (what you ask for),
*implementation* (what you get) and *lifetime*. `builder.Services` is just a list of them.

## Several implementations of one interface
Register the same interface twice and:
- asking for `INotificationSender` gives the **last** one registered;
- asking for `IEnumerable<INotificationSender>` gives **all** of them, in registration order.

`TryAddSingleton<I, T>()` registers only if nothing is registered for `I` yet (libraries use it so they don't overwrite yours).

## Clean code with extension methods
Move groups of registrations into an extension method on `IServiceCollection`, so `Program.cs` reads like a table of contents:
```csharp
public static IServiceCollection AddDramaSchoolServices(this IServiceCollection services) { ...; return services; }
builder.Services.AddDramaSchoolServices();
```
Returning `services` allows chaining. The framework's own `AddControllersWithViews()` is exactly this.

## Keyed services (.NET 8+)
Register under a key, ask for it by key:
```csharp
builder.Services.AddKeyedSingleton<INotificationSender, SmsSender>("urgent");
([FromKeyedServices("urgent")] INotificationSender sender) => ...
```
Keyed registrations are not included when you ask without a key.

## Third-party containers
The built-in container covers most apps. Libraries like **Scrutor** add assembly scanning and decorators on top of it;
full containers like **Autofac** replace it (`builder.Host.UseServiceProviderFactory(...)`) for modules, property injection
and more. Reach for them only when you hit a real limit.

## Steps
| Step | Course clip | What you do |
|---|---|---|
| 1 | Demo: Registering Multiple Implementations | `SmsSender`; register both senders |
| 2 | Demo: Clean Code with Extension Methods | `AddDramaSchoolServices()` |
| 3 | Advanced Techniques | Keyed "urgent" sender and `/api/notify/urgent` |

Check your work: `dotnet test --filter Module06`
