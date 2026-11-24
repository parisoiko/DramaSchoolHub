using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using DramaSchoolHub.Controllers;
using DramaSchoolHub.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DramaSchoolHub.Tests;

// Module 3: Injecting Services.
// GetById is called through reflection so this file compiles before you add it.
public class Module03Tests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public Module03Tests(WebApplicationFactory<Program> factory) => _factory = factory;

    private static async Task<string> GetPage(HttpClient client, string path, string hint)
    {
        HttpResponseMessage response = await client.GetAsync(path);
        string body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"GET {path} answered {(int)response.StatusCode} {response.StatusCode}. {hint}");
        return body;
    }

    // ---------- 1. A second method on the interface ----------

    [Fact]
    public void Module03_Step1_CatalogueFindsOneRoom()
    {
        Type contract = typeof(Program).Assembly.GetType("DramaSchoolHub.Services.IRoomCatalogue")!;
        MethodInfo? getById = contract.GetMethod("GetById", [typeof(int)]);
        Assert.True(getById != null, "IRoomCatalogue needs a method GetById(int id).");
        Assert.True(getById!.ReturnType == typeof(RehearsalRoom),
            $"GetById should return RehearsalRoom? (one room, or null), not {getById.ReturnType.Name}.");

        object catalogue = _factory.Services.GetRequiredService(contract);
        var studioA = (RehearsalRoom?)getById.Invoke(catalogue, [2]);
        Assert.True(studioA?.Name == "Studio A", $"GetById(2) should be Studio A, but returned {studioA?.Name ?? "null"}.");

        object? missing = getById.Invoke(catalogue, [99]);
        Assert.True(missing == null, "GetById(99) should return null: there's no room 99. (FirstOrDefault, not First.)");
    }

    // ---------- 2. Action injection ----------

    [Fact]
    public async Task Module03_Step2_RoomActionGetsTheRulesWithFromServices()
    {
        MethodInfo? room = typeof(HomeController).GetMethod("Room");
        Assert.True(room != null, "HomeController has no Room action yet.");
        Assert.True(room!.GetParameters().Any(p => p.GetCustomAttribute<FromServicesAttribute>() != null),
            "Room should get the booking rules through a [FromServices] parameter, not through the constructor.");
        Assert.True(typeof(HomeController).GetConstructors().All(c => c.GetParameters().Length == 2),
            "HomeController's constructor should still take only the catalogue and the logger: " +
            "the rules are needed by one action only.");

        HttpClient client = _factory.CreateClient();
        string body = await GetPage(client, "/Home/Room/2", "Run the app (F5) and open /Home/Room/2 to see the error.");
        Assert.Contains("Studio A", body);
        Assert.Contains("5 hours", body);

        HttpResponseMessage missing = await client.GetAsync("/Home/Room/99");
        Assert.True(missing.StatusCode == HttpStatusCode.NotFound,
            $"/Home/Room/99 answered {(int)missing.StatusCode}: for a room that doesn't exist, return NotFound().");
    }

    // ---------- 3. Minimal API ----------

    [Fact]
    public async Task Module03_Step3_ApiListsTheRooms()
    {
        HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/api/rooms");
        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"GET /api/rooms answered {(int)response.StatusCode}. Add app.MapGet(\"/api/rooms\", ...) in Program.cs.");

        List<RehearsalRoom>? rooms = await response.Content.ReadFromJsonAsync<List<RehearsalRoom>>();
        Assert.True(rooms?.Count == 4, $"/api/rooms should return the 4 rooms, but returned {rooms?.Count ?? 0}.");
        Assert.Equal("Main Stage", rooms![0].Name);
    }

    [Fact]
    public async Task Module03_Step3_ApiFindsOneRoom()
    {
        HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/api/rooms/4");
        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"GET /api/rooms/4 answered {(int)response.StatusCode}. Add app.MapGet(\"/api/rooms/{{id}}\", ...).");
        RehearsalRoom? room = await response.Content.ReadFromJsonAsync<RehearsalRoom>();
        Assert.Equal("Black Box", room?.Name);

        HttpResponseMessage missing = await client.GetAsync("/api/rooms/99");
        Assert.True(missing.StatusCode == HttpStatusCode.NotFound,
            $"/api/rooms/99 answered {(int)missing.StatusCode}: return Results.NotFound() when GetById gives null.");
    }

    // ---------- 4. @inject in a view ----------

    [Fact]
    public async Task Module03_Step4_FooterShowsOpeningHoursOnEveryPage()
    {
        HttpClient client = _factory.CreateClient();
        foreach (string path in new[] { "/", "/Lifetimes" })
        {
            string body = await GetPage(client, path, "Check the @inject line in _Layout.cshtml.");
            Assert.True(body.Contains("<footer") && body.Contains("09:00") && body.Contains("22:00"),
                $"{path} has no <footer> with the opening hours (09:00 to 22:00) yet. Add it to Views/Shared/_Layout.cshtml.");
        }

        // Change the setting: the footer must follow it, not hard-coded hours.
        HttpClient changed = _factory
            .WithWebHostBuilder(b => b.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?> { ["BookingRules:OpeningHour"] = "8" })))
            .CreateClient();
        string changedBody = await GetPage(changed, "/", "Check the @inject line in _Layout.cshtml.");
        Assert.True(changedBody.Contains("08:00"),
            "With OpeningHour set to 8 the footer doesn't say 08:00: read the hours from the injected rules (Rules.Value...).");
    }
}
