using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;
using DramaSchoolHub.Models;
using DramaSchoolHub.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DramaSchoolHub.Tests;

// Module 4: Avoiding Dependency Injection Pitfalls.
// New types are looked up BY NAME so this file compiles before they exist.
public class Module04Tests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public Module04Tests(WebApplicationFactory<Program> factory) => _factory = factory;

    private static Type RequireType(string fullName, string hint)
    {
        Type? type = typeof(Program).Assembly.GetType(fullName);
        Assert.True(type != null, $"No class {fullName} yet. {hint}");
        return type!;
    }

    // Starting the app is where DI mistakes show up; turn that crash into a readable failure.
    private static IServiceProvider StartedServices(WebApplicationFactory<Program> factory)
    {
        try
        {
            return factory.Services;
        }
        catch (Exception e)
        {
            string message = e.GetBaseException().Message;
            Assert.Fail("The app doesn't start: " + message +
                (message.Contains("Cannot consume scoped service")
                    ? "  ← That's the captive dependency: a singleton can't take a scoped service. Register Usher with AddScoped."
                    : ""));
            throw;
        }
    }

    private static Type RequireUsher() =>
        RequireType("DramaSchoolHub.Services.Usher", "Create Services/Usher.cs (step 1).");

    // ---------- 1. Captive dependencies and scope validation ----------

    [Fact]
    public void Module04_Step1_UsherIsScopedLikeItsPass()
    {
        Type usher = RequireUsher();
        Assert.True(usher.GetConstructors().Any(c => c.GetParameters().Any(p => p.ParameterType == typeof(ScopedPass))),
            "Usher's constructor should take a ScopedPass.");

        IServiceProvider services = StartedServices(_factory);
        using IServiceScope request1 = services.CreateScope();
        using IServiceScope request2 = services.CreateScope();
        object? first = request1.ServiceProvider.GetService(usher);
        Assert.True(first != null, "The DI container doesn't know Usher. Register it in Program.cs (MODULE 4, step 1).");
        Assert.True(ReferenceEquals(first, request1.ServiceProvider.GetService(usher)),
            "Asking twice for an Usher in one request gave two objects: use AddScoped, not AddTransient.");
        Assert.True(!ReferenceEquals(first, request2.ServiceProvider.GetService(usher)),
            "Two requests got the same Usher: register it with AddScoped.");
    }

    // ---------- 2. Thread safety in a singleton ----------

    [Fact]
    public void Module04_Step2_BookingStoreIsARegisteredSingleton()
    {
        IServiceProvider services = StartedServices(_factory);
        using IServiceScope request1 = services.CreateScope();
        using IServiceScope request2 = services.CreateScope();
        IBookingStore? store = request1.ServiceProvider.GetService<IBookingStore>();
        Assert.True(store != null, "The DI container doesn't know IBookingStore. Register it in Program.cs (MODULE 4, step 2).");
        Assert.True(ReferenceEquals(store, request2.ServiceProvider.GetService<IBookingStore>()),
            "Two requests got different booking stores, so each would see different bookings: register it with AddSingleton.");
    }

    [Fact]
    public void Module04_Step2_BookingStoreSurvivesManyUsersAtOnce()
    {
        var store = new BookingStore();
        int before = store.GetAll().Count;
        const int users = 20_000;

        try
        {
            // Many "requests" at the same moment: half add a booking, half read the list.
            Parallel.For(0, users * 2, i =>
            {
                if (i % 2 == 0)
                    store.Add(new Booking { RoomId = 3, BookedBy = $"Student {i}", Start = DateTime.Today.AddDays(3), Hours = 1 });
                else
                    store.GetAll();
            });
        }
        catch (AggregateException e)
        {
            Assert.Fail($"Two threads used the store at the same time and it crashed: {e.InnerExceptions[0].Message} " +
                "Wrap the body of each method in lock (_lock) { ... }.");
        }

        List<Booking> all = store.GetAll();
        Assert.True(all.Count == before + users,
            $"{users} bookings were added but the store holds {all.Count - before}: some were lost when two threads " +
            "changed the list at once. Use lock (_lock) in Add.");
        int distinctIds = all.Select(b => b.Id).Distinct().Count();
        Assert.True(distinctIds == all.Count,
            $"{all.Count - distinctIds} bookings share an Id with another one: _nextId++ isn't safe with two threads. " +
            "Put it inside the lock too.");
    }

    // ---------- 3. Options: validation and lifetimes ----------

    private static string StartupError(WebApplicationFactory<Program> factory, Dictionary<string, string?> settings)
    {
        try
        {
            using WebApplicationFactory<Program> broken = factory.WithWebHostBuilder(b =>
                b.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(settings)));
            broken.CreateClient();
            return "";
        }
        catch (Exception e)
        {
            return e.GetBaseException().Message;
        }
    }

    [Fact]
    public void Module04_Step3_BadBookingRulesStopTheApp()
    {
        PropertyInfo maxHours = typeof(BookingRules).GetProperty("MaxBookingHours")!;
        Assert.True(maxHours.GetCustomAttribute<RangeAttribute>() != null,
            "BookingRules.MaxBookingHours needs a [Range(1, 8)] attribute.");

        string zeroHours = StartupError(_factory, new() { ["BookingRules:MaxBookingHours"] = "0" });
        Assert.True(zeroHours.Contains("MaxBookingHours"),
            "With MaxBookingHours = 0 the app should refuse to start, naming MaxBookingHours. " +
            (zeroHours == "" ? "It started anyway: are ValidateDataAnnotations() and ValidateOnStart() in Program.cs?"
                             : $"It failed with: {zeroHours}"));

        string backwards = StartupError(_factory, new() { ["BookingRules:OpeningHour"] = "23" });
        Assert.True(backwards != "",
            "With OpeningHour 23 and ClosingHour 22 the app should refuse to start: add " +
            ".Validate(rules => rules.OpeningHour < rules.ClosingHour, \"...\").");

        Assert.True(StartupError(_factory, []) == "", "With the normal settings the app should start.");
    }

    [Fact]
    public void Module04_Step3_RulesPageUsesASnapshot()
    {
        Type controller = RequireType("DramaSchoolHub.Controllers.RulesController", "");
        bool usesSnapshot = controller.GetConstructors()
            .Any(c => c.GetParameters().Any(p => p.ParameterType == typeof(IOptionsSnapshot<BookingRules>)));
        Assert.True(usesSnapshot, "RulesController should ask for IOptionsSnapshot<BookingRules> instead of IOptions<BookingRules>.");
    }

    // ---------- 4. Disposal ----------

    [Fact]
    public void Module04_Step4_UsherIsDisposedWhenTheRequestEnds()
    {
        Type usher = RequireUsher();
        Assert.True(typeof(IDisposable).IsAssignableFrom(usher), "Usher should implement IDisposable: public class Usher(...) : ____");
        PropertyInfo? isDisposed = usher.GetProperty("IsDisposed");
        Assert.True(isDisposed?.PropertyType == typeof(bool), "Usher needs a bool IsDisposed property.");

        object instance;
        using (IServiceScope request = StartedServices(_factory).CreateScope())
        {
            instance = request.ServiceProvider.GetRequiredService(usher);
            Assert.False((bool)isDisposed!.GetValue(instance)!, "IsDisposed is already true before the request ended.");
        } // the request ends here

        Assert.True((bool)isDisposed.GetValue(instance)!, "The request ended but IsDisposed is still false: set it to true in Dispose().");
    }

    // ---------- 5. A background service ----------

    [Fact]
    public async Task Module04_Step5_CleanupServiceRemovesEndedBookings()
    {
        Type cleanup = RequireType("DramaSchoolHub.Services.BookingCleanupService", "Create Services/BookingCleanupService.cs (step 5).");
        Assert.True(cleanup.IsSubclassOf(typeof(BackgroundService)), "BookingCleanupService should inherit from BackgroundService.");

        IServiceProvider services = StartedServices(_factory);
        Assert.True(services.GetServices<IHostedService>().Any(s => s.GetType() == cleanup),
            "BookingCleanupService isn't registered: builder.Services.AddHostedService<____>();");

        HttpClient client = _factory.CreateClient();
        string body = "";
        for (int attempt = 0; attempt < 50; attempt++)
        {
            body = await client.GetStringAsync("/api/bookings");
            if (!body.Contains("Chekhov")) break;
            await Task.Delay(100);
        }
        Assert.True(!body.Contains("Chekhov"),
            "Yesterday's booking (Chekhov scene study) is still there 5 seconds after start-up: " +
            "does ExecuteAsync clean up once straight away, before waiting for the timer?");
        Assert.Contains("Hamlet", body);
    }
}
