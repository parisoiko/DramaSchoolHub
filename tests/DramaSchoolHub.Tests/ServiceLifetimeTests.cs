using System.Text.RegularExpressions;
using DramaSchoolHub.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DramaSchoolHub.Tests;

// Transient, scoped and singleton behaviour, the captive-dependency check, and disposal.
public class ServiceLifetimeTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public void Passes_AreRegisteredWithTheirLifetimes()
    {
        using IServiceScope request1 = factory.Services.CreateScope();
        using IServiceScope request2 = factory.Services.CreateScope();
        T Get<T>(IServiceScope scope) where T : notnull => scope.ServiceProvider.GetRequiredService<T>();

        Assert.NotSame(Get<TransientPass>(request1), Get<TransientPass>(request1));

        Assert.Same(Get<ScopedPass>(request1), Get<ScopedPass>(request1));
        Assert.NotSame(Get<ScopedPass>(request1), Get<ScopedPass>(request2));

        Assert.Same(Get<SingletonPass>(request1), Get<SingletonPass>(request2));
    }

    [Fact]
    public async Task LifetimesPage_ShowsTheDifferenceAcrossRequests()
    {
        HttpClient client = factory.CreateClient();
        string firstBody = await client.GetStringAsync("/Lifetimes");
        var first = ReadRows(firstBody);
        var second = ReadRows(await client.GetStringAsync("/Lifetimes"));

        Assert.NotEqual(first["Transient"].First, first["Transient"].Second);
        Assert.Equal(first["Scoped"].First, first["Scoped"].Second);
        Assert.Equal(first["Singleton"].First, first["Singleton"].Second);

        Assert.NotEqual(first["Scoped"].First, second["Scoped"].First);
        Assert.Equal(first["Singleton"].First, second["Singleton"].First);

        // The usher is a separate scoped service, so it got the same pass as the controller.
        Assert.Contains($"Welcome, pass {first["Scoped"].First}!", firstBody);
    }

    [Fact]
    public void SingletonUsher_IsRejectedAsACaptiveDependency()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILogger<Usher>>(NullLogger<Usher>.Instance);
        services.AddScoped<ScopedPass>();
        services.AddSingleton<Usher>();

        var error = Assert.Throws<AggregateException>(() =>
            services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true }));
        Assert.Contains("Cannot consume scoped service", error.Message);
    }

    [Fact]
    public void Usher_IsDisposedWhenTheRequestEnds()
    {
        Usher usher;
        using (IServiceScope request = factory.Services.CreateScope())
        {
            usher = request.ServiceProvider.GetRequiredService<Usher>();
            Assert.False(usher.IsDisposed);
        }

        Assert.True(usher.IsDisposed);
    }

    private static Dictionary<string, (string First, string Second)> ReadRows(string body) =>
        Regex.Matches(body, "data-lifetime=\"(\\w+)\" data-first=\"(\\w+)\" data-second=\"(\\w+)\"")
            .ToDictionary(m => m.Groups[1].Value, m => (m.Groups[2].Value, m.Groups[3].Value));
}
