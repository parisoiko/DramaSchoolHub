using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using DramaSchoolHub.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DramaSchoolHub.Tests;

// Module 6: Advanced Patterns.
public class Module06Tests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public Module06Tests(WebApplicationFactory<Program> factory) => _factory = factory;

    private async Task<List<string>> Notify(string path)
    {
        HttpResponseMessage response = await _factory.CreateClient().GetAsync(path);
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"GET {path} answered {(int)response.StatusCode}.");
        return await response.Content.ReadFromJsonAsync<List<string>>() ?? [];
    }

    // ---------- 1. Several implementations of one interface ----------

    [Fact]
    public async Task Module06_Step1_BothSendersAreUsed()
    {
        Assert.True(typeof(Program).Assembly.GetType("DramaSchoolHub.Notifications.SmsSender") != null,
            "No class DramaSchoolHub.Notifications.SmsSender yet. Create Notifications/SmsSender.cs.");

        List<string> sent = await Notify("/api/notify");
        Assert.True(sent.Count == 2 && sent.Any(s => s.StartsWith("Email")) && sent.Any(s => s.StartsWith("SMS")),
            $"/api/notify should send one email and one SMS, but sent: [{string.Join(" | ", sent)}]. " +
            "Register both: builder.Services.AddSingleton<INotificationSender, ____>(); twice.");
    }

    // ---------- 2. An extension method for the registrations ----------

    [Fact]
    public void Module06_Step2_ExtensionMethodRegistersTheSchoolServices()
    {
        MethodInfo? method = typeof(Program).Assembly.GetTypes()
            .Where(t => t.IsAbstract && t.IsSealed) // static classes
            .Select(t => t.GetMethod("AddDramaSchoolServices"))
            .FirstOrDefault(m => m != null);
        Assert.True(method != null,
            "No AddDramaSchoolServices method yet. Create it in a static class in Extensions/ServiceCollectionExtensions.cs.");

        Assert.True(method!.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute)),
            "AddDramaSchoolServices isn't an extension method yet: put `this` before its first parameter.");

        var services = new ServiceCollection();
        object?[] args = method!.GetParameters().Length == 2
            ? [services, new ConfigurationBuilder().Build()]
            : [services];
        method.Invoke(null, args);

        foreach (Type expected in new[] { typeof(IRoomCatalogue), typeof(IBookingStore), typeof(Usher), typeof(ScopedPass) })
            Assert.True(services.Any(d => d.ServiceType == expected),
                $"AddDramaSchoolServices doesn't register {expected.Name}. Move the Module 1-4 registrations into it.");
    }

    // ---------- 3. Keyed services ----------

    [Fact]
    public async Task Module06_Step3_UrgentNotificationsGoBySmsOnly()
    {
        List<string> sent = await Notify("/api/notify/urgent");
        Assert.True(sent.Count == 1 && sent[0].StartsWith("SMS"),
            $"/api/notify/urgent should send exactly one SMS, but sent: [{string.Join(" | ", sent)}].");

        List<string> all = await Notify("/api/notify");
        Assert.True(all.Count == 2, "The keyed registration shouldn't change /api/notify: it should still send 2 notifications.");
    }
}
