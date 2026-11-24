using System.Net;
using System.Reflection;
using DramaSchoolHub.Controllers;
using DramaSchoolHub.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DramaSchoolHub.Tests;

// Module 1: Getting Started with Dependency Injection.
// Your types are looked up BY NAME, so these tests compile even before
// you create them; a missing type fails with a message saying what to add.
public class Module01Tests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public Module01Tests(WebApplicationFactory<Program> factory) => _factory = factory;

    private static Type RequireCatalogueInterface()
    {
        Type? type = typeof(Program).Assembly.GetType("DramaSchoolHub.Services.IRoomCatalogue");
        Assert.True(type is { IsInterface: true },
            "No interface DramaSchoolHub.Services.IRoomCatalogue yet. Create Services/IRoomCatalogue.cs (step 1).");
        return type!;
    }

    private static async Task<string> GetHomePage(HttpClient client)
    {
        HttpResponseMessage response = await client.GetAsync("/");
        string body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"GET / answered {(int)response.StatusCode} {response.StatusCode}. " +
            "A 500 here usually means \"Unable to resolve service\": the controller asks for something " +
            "that isn't registered in Program.cs. Run the app (F5) to see the full error page.");
        return body;
    }

    // ---------- 0. The starting app works ----------

    [Fact]
    public async Task Module01_Step0_HomePageListsTheRooms()
    {
        string body = await GetHomePage(_factory.CreateClient());
        Assert.Contains("Main Stage", body);
        Assert.Contains("Black Box", body);
    }

    // ---------- 1. Coding to interfaces ----------

    [Fact]
    public void Module01_Step1_RoomCatalogueImplementsAnInterface()
    {
        Type contract = RequireCatalogueInterface();

        MethodInfo? getAll = contract.GetMethod("GetAll");
        Assert.True(getAll != null, "IRoomCatalogue needs a GetAll() method.");
        Assert.True(getAll!.ReturnType == typeof(List<RehearsalRoom>),
            $"IRoomCatalogue.GetAll() should return List<RehearsalRoom>, not {getAll.ReturnType.Name}.");

        Type? catalogue = typeof(Program).Assembly.GetType("DramaSchoolHub.Services.RoomCatalogue");
        Assert.True(catalogue != null && contract.IsAssignableFrom(catalogue),
            "RoomCatalogue doesn't implement IRoomCatalogue yet: public class RoomCatalogue : ____");
    }

    // ---------- 2. Constructor injection ----------

    [Fact]
    public void Module01_Step2_HomeControllerAsksForTheCatalogue()
    {
        Type contract = RequireCatalogueInterface();

        bool asks = typeof(HomeController).GetConstructors()
            .Any(c => c.GetParameters().Any(p => p.ParameterType == contract));
        Assert.True(asks, "HomeController needs a constructor with an IRoomCatalogue parameter.");
    }

    // ---------- 3. Registering the service ----------

    [Fact]
    public async Task Module01_Step3_CatalogueIsRegistered()
    {
        Type contract = RequireCatalogueInterface();

        using IServiceScope scope = _factory.Services.CreateScope();
        Assert.True(scope.ServiceProvider.GetService(contract) != null,
            "The DI container doesn't know IRoomCatalogue. Register it in Program.cs: " +
            "builder.Services.Add____<IRoomCatalogue, RoomCatalogue>();");

        string body = await GetHomePage(_factory.CreateClient());
        Assert.Contains("Main Stage", body);
    }

    [Fact]
    public async Task Module01_Step3_ControllerUsesTheInjectedCatalogue()
    {
        Type contract = RequireCatalogueInterface();

        // Replace the real catalogue with a fake one that only has "Test Room".
        object fake = DispatchProxy.Create(contract, typeof(FakeCatalogue));
        HttpClient client = _factory
            .WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton(contract, fake)))
            .CreateClient();

        string body = await GetHomePage(client);
        Assert.True(body.Contains("Test Room") && !body.Contains("Main Stage"),
            "The page still shows the real rooms, so Index isn't using the injected catalogue. " +
            "Is there still a `new RoomCatalogue()` in HomeController?");
    }

    // ---------- 4. Injecting a framework dependency ----------

    [Fact]
    public async Task Module01_Step4_HomeControllerLogsTheRoomCount()
    {
        bool asksForLogger = typeof(HomeController).GetConstructors()
            .Any(c => c.GetParameters().Any(p => p.ParameterType == typeof(ILogger<HomeController>)));
        Assert.True(asksForLogger, "HomeController's constructor should also take an ILogger<HomeController>.");

        var logs = new CollectingLoggerProvider();
        HttpClient client = _factory
            .WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton<ILoggerProvider>(logs)))
            .CreateClient();
        await GetHomePage(client);

        List<string> fromController = logs.Messages
            .Where(m => m.Category == typeof(HomeController).FullName)
            .Select(m => m.Text)
            .ToList();
        Assert.True(fromController.Any(text => text.Contains('4')),
            "Index should log how many rooms it shows (e.g. \"Showing 4 rehearsal rooms\"). " +
            $"Logged by HomeController: [{string.Join(", ", fromController)}]");
    }

    // ---------- helpers ----------

    // Stands in for IRoomCatalogue at runtime (the interface doesn't exist when this file compiles).
    public class FakeCatalogue : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == "GetAll"
                ? new List<RehearsalRoom> { new() { Id = 99, Name = "Test Room", Capacity = 5, HasPiano = false } }
                : throw new NotSupportedException(targetMethod?.Name);
    }

    private sealed class CollectingLoggerProvider : ILoggerProvider
    {
        public List<(string Category, string Text)> Messages { get; } = [];

        public ILogger CreateLogger(string categoryName) => new CollectingLogger(categoryName, Messages);

        public void Dispose() { }

        private sealed class CollectingLogger(string category, List<(string, string)> messages) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                lock (messages) messages.Add((category, formatter(state, exception)));
            }
        }
    }
}
