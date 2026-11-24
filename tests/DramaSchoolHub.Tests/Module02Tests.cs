using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DramaSchoolHub.Tests;

// Module 2: Service Lifetimes and Configuration.
// As in Module 1, your types are looked up BY NAME so this file compiles before they exist.
public class Module02Tests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public Module02Tests(WebApplicationFactory<Program> factory) => _factory = factory;

    private static Type RequireType(string fullName, string hint)
    {
        Type? type = typeof(Program).Assembly.GetType(fullName);
        Assert.True(type != null, $"No class {fullName} yet. {hint}");
        return type!;
    }

    private static Type RequirePass(string name) =>
        RequireType($"DramaSchoolHub.Services.{name}", "Create Services/StagePass.cs (step 1).");

    private static string CodeOf(object pass) =>
        (string)pass.GetType().GetProperty("Code")!.GetValue(pass)!;

    private static async Task<string> GetPage(HttpClient client, string path, string hint)
    {
        HttpResponseMessage response = await client.GetAsync(path);
        string body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"GET {path} answered {(int)response.StatusCode} {response.StatusCode}. {hint}");
        return body;
    }

    // ---------- 1. Three kinds of stage pass ----------

    [Fact]
    public void Module02_Step1_StagePassesHaveRandomCodes()
    {
        Type basePass = RequirePass("StagePass");
        PropertyInfo? code = basePass.GetProperty("Code");
        Assert.True(code?.PropertyType == typeof(string), "StagePass needs a string property called Code.");

        foreach (string name in new[] { "TransientPass", "ScopedPass", "SingletonPass" })
        {
            Type pass = RequirePass(name);
            Assert.True(pass.IsSubclassOf(basePass), $"{name} should inherit from StagePass: public class {name} : ____ {{ }}");

            string first = CodeOf(Activator.CreateInstance(pass)!);
            string second = CodeOf(Activator.CreateInstance(pass)!);
            Assert.True(first.Length == 6, $"Code should be 6 characters long, but a {name} got \"{first}\".");
            Assert.True(first != second, $"Two new {name} objects got the same code \"{first}\": each pass needs its own random code.");
        }
    }

    // ---------- 2. Registering with three lifetimes ----------

    [Fact]
    public void Module02_Step2_PassesAreRegisteredWithTheRightLifetimes()
    {
        Type transient = RequirePass("TransientPass");
        Type scoped = RequirePass("ScopedPass");
        Type singleton = RequirePass("SingletonPass");

        using IServiceScope request1 = _factory.Services.CreateScope();
        using IServiceScope request2 = _factory.Services.CreateScope();

        object? Get(IServiceScope scope, Type type)
        {
            object? pass = scope.ServiceProvider.GetService(type);
            Assert.True(pass != null,
                $"The DI container doesn't know {type.Name}. Register it in Program.cs (MODULE 2, step 2).");
            return pass;
        }

        Assert.True(!ReferenceEquals(Get(request1, transient), Get(request1, transient)),
            "Asking twice for a TransientPass gave the same object: register it with AddTransient.");

        Assert.True(ReferenceEquals(Get(request1, scoped), Get(request1, scoped)),
            "Asking twice for a ScopedPass in ONE request gave two objects: register it with AddScoped.");
        Assert.True(!ReferenceEquals(Get(request1, scoped), Get(request2, scoped)),
            "Two requests got the same ScopedPass: register it with AddScoped, not AddSingleton.");

        Assert.True(ReferenceEquals(Get(request1, singleton), Get(request2, singleton)),
            "Two requests got different SingletonPass objects: register it with AddSingleton.");
    }

    // ---------- 3. The Lifetimes page ----------

    private static Dictionary<string, (string First, string Second)> ReadRows(string body) =>
        Regex.Matches(body, "data-lifetime=\"(\\w+)\" data-first=\"(\\w*)\" data-second=\"(\\w*)\"")
            .ToDictionary(m => m.Groups[1].Value, m => (m.Groups[2].Value, m.Groups[3].Value));

    [Fact]
    public async Task Module02_Step3_LifetimesPageShowsTheDifference()
    {
        RequireType("DramaSchoolHub.Controllers.LifetimesController", "Create Controllers/LifetimesController.cs (step 3).");

        HttpClient client = _factory.CreateClient();
        const string hint = "A 500 usually means a pass isn't registered (step 2). Run the app (F5) to see the error page.";
        var first = ReadRows(await GetPage(client, "/Lifetimes", hint));
        var second = ReadRows(await GetPage(client, "/Lifetimes", hint));

        foreach (string lifetime in new[] { "Transient", "Scoped", "Singleton" })
            Assert.True(first.ContainsKey(lifetime),
                $"The page has no \"{lifetime}\" row. Index should build three LifetimeRow objects: \"Transient\", \"Scoped\", \"Singleton\".");

        Assert.True(first["Transient"].First != first["Transient"].Second,
            "The Transient row shows the same code twice: pass transient1.Code and transient2.Code (two different parameters).");
        Assert.True(first["Scoped"].First == first["Scoped"].Second,
            "The Scoped row shows two different codes: does it use scoped1 and scoped2?");
        Assert.True(first["Singleton"].First == first["Singleton"].Second,
            "The Singleton row shows two different codes: does it use singleton1 and singleton2?");

        Assert.True(first["Scoped"].First != second["Scoped"].First,
            "The Scoped code didn't change between two requests.");
        Assert.True(first["Singleton"].First == second["Singleton"].First,
            "The Singleton code changed between two requests.");
    }

    // ---------- 4. Booking rules in appsettings.json ----------

    private static Type RequireBookingRules() =>
        RequireType("DramaSchoolHub.Models.BookingRules", "Create Models/BookingRules.cs (step 4).");

    [Fact]
    public void Module02_Step4_BookingRulesAreInAppSettings()
    {
        Type rules = RequireBookingRules();
        foreach (string name in new[] { "OpeningHour", "ClosingHour", "MaxBookingHours" })
            Assert.True(rules.GetProperty(name)?.PropertyType == typeof(int),
                $"BookingRules needs an int property called {name} (with {{ get; set; }}).");

        IConfigurationSection section = _factory.Services.GetRequiredService<IConfiguration>().GetSection("BookingRules");
        Assert.True(section.Exists(), "appsettings.json has no \"BookingRules\" section yet.");

        object? bound = section.Get(rules);
        int Read(string name) => (int)rules.GetProperty(name)!.GetValue(bound)!;
        Assert.True(bound != null && Read("OpeningHour") == 9 && Read("ClosingHour") == 22 && Read("MaxBookingHours") == 5,
            "The \"BookingRules\" section should hold OpeningHour 9, ClosingHour 22 and MaxBookingHours 5. " +
            "Check the spelling: JSON names must match the property names.");
    }

    // ---------- 5. The Options pattern ----------

    [Fact]
    public void Module02_Step5_BookingRulesAreBoundWithOptions()
    {
        Type rules = RequireBookingRules();
        object options = _factory.Services.GetRequiredService(typeof(IOptions<>).MakeGenericType(rules));
        object value = options.GetType().GetProperty("Value")!.GetValue(options)!;

        Assert.True((int)rules.GetProperty("MaxBookingHours")!.GetValue(value)! == 5,
            "IOptions<BookingRules> still has MaxBookingHours 0, so the section isn't bound. In Program.cs: " +
            "builder.Services.Configure<BookingRules>(builder.Configuration.GetSection(\"BookingRules\"));");
    }

    [Fact]
    public async Task Module02_Step5_RulesPageReadsTheConfiguration()
    {
        RequireType("DramaSchoolHub.Controllers.RulesController", "Create Controllers/RulesController.cs (step 5).");
        const string hint = "Is there a Views/Rules/Index.cshtml? Run the app (F5) to see the error page.";

        string body = await GetPage(_factory.CreateClient(), "/Rules", hint);
        Assert.Contains("09:00", body);
        Assert.Contains("22:00", body);
        Assert.Contains("up to 5 hours", body);

        // Change the setting: the page must follow it, not a hard-coded 5.
        HttpClient changed = _factory
            .WithWebHostBuilder(b => b.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?> { ["BookingRules:MaxBookingHours"] = "4" })))
            .CreateClient();
        string changedBody = await GetPage(changed, "/Rules", hint);
        Assert.True(changedBody.Contains("up to 4 hours"),
            "With MaxBookingHours set to 4 the page still doesn't say \"up to 4 hours\": " +
            "the controller should pass options.Value to the view.");
    }
}
