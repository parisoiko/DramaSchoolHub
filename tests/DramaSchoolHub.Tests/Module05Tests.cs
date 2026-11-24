using System.Net;
using System.Reflection;
using DramaSchoolHub.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DramaSchoolHub.Tests;

// Module 5: Understanding the ASP.NET Core Request Pipeline.
// New types are looked up BY NAME so this file compiles before they exist.
public class Module05Tests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public Module05Tests(WebApplicationFactory<Program> factory) => _factory = factory;

    private static Type RequireType(string fullName, string hint)
    {
        Type? type = typeof(Program).Assembly.GetType(fullName);
        Assert.True(type != null, $"No class {fullName} yet. {hint}");
        return type!;
    }

    private HttpClient ClientWithLogs(CollectingLoggerProvider logs) => _factory
        .WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton<ILoggerProvider>(logs)))
        .CreateClient();

    private HttpClient ClientWithBoxOffice(bool closed) => _factory
        .WithWebHostBuilder(b => b.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BookingRules:BoxOfficeClosed"] = closed ? "true" : "false"
            })))
        .CreateClient();

    // ---------- 1. Inline middleware ----------

    [Fact]
    public async Task Module05_Step1_EveryResponseCarriesTheTheatreHeader()
    {
        HttpClient client = _factory.CreateClient();
        foreach (string path in new[] { "/", "/api/rooms", "/css/site.css" })
        {
            HttpResponseMessage response = await client.GetAsync(path);
            Assert.True(response.Headers.TryGetValues("X-Theatre", out var values) && values.Single() == "DramaSchoolHub",
                $"The response to {path} has no X-Theatre: DramaSchoolHub header. " +
                "Add it in an app.Use(...) at the top of the pipeline, BEFORE calling next.");
        }
    }

    // ---------- 2. A middleware class ----------

    [Fact]
    public async Task Module05_Step2_RequestTimerLogsEveryRequest()
    {
        Type timer = RequireType("DramaSchoolHub.Middleware.RequestTimingMiddleware",
            "Create Middleware/RequestTimingMiddleware.cs (step 2).");
        Assert.True(timer.GetMethod("InvokeAsync") != null, "RequestTimingMiddleware needs a public InvokeAsync(HttpContext context) method.");

        var logs = new CollectingLoggerProvider();
        await ClientWithLogs(logs).GetAsync("/Lifetimes");

        List<string> fromTimer = logs.From(timer);
        Assert.True(fromTimer.Any(text => text.Contains("/Lifetimes") &&
                System.Text.RegularExpressions.Regex.IsMatch(text, @"took \d+ ms")),
            "After GET /Lifetimes the timer should log something like \"GET /Lifetimes took 12 ms\". " +
            $"It logged: [{string.Join(", ", fromTimer)}]. Is it registered with app.UseMiddleware<RequestTimingMiddleware>()?");
    }

    // ---------- 3. Short-circuiting: the box office ----------

    [Fact]
    public async Task Module05_Step3_ClosedBoxOfficeTurnsVisitorsAway()
    {
        Assert.True(typeof(BookingRules).GetProperty("BoxOfficeClosed")?.PropertyType == typeof(bool),
            "BookingRules needs a bool BoxOfficeClosed property (and \"BoxOfficeClosed\": false in appsettings.json).");
        Type boxOffice = RequireType("DramaSchoolHub.Middleware.BoxOfficeMiddleware", "Create Middleware/BoxOfficeMiddleware.cs (step 3).");

        HttpClient closed = ClientWithBoxOffice(closed: true);
        foreach (string path in new[] { "/", "/api/rooms" })
        {
            HttpResponseMessage response = await closed.GetAsync(path);
            string body = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.ServiceUnavailable && body.Contains("closed"),
                $"With BoxOfficeClosed = true, {path} answered {(int)response.StatusCode} instead of 503 with a \"closed\" message. " +
                "Is app.UseMiddleware<BoxOfficeMiddleware>() in the pipeline, before UseRouting?");
        }

        HttpResponseMessage css = await closed.GetAsync("/css/site.css");
        Assert.True(css.StatusCode == HttpStatusCode.OK,
            $"With the box office closed, /css/site.css answered {(int)css.StatusCode}: the closed page has no styling. " +
            "Order matters: put the box office AFTER app.UseStaticFiles().");

        HttpResponseMessage open = await ClientWithBoxOffice(closed: false).GetAsync("/");
        Assert.True(open.StatusCode == HttpStatusCode.OK, "With BoxOfficeClosed = false the home page should work normally.");
    }

    [Fact]
    public void Module05_Step3_BoxOfficeReadsTheRulesPerRequest()
    {
        Type boxOffice = RequireType("DramaSchoolHub.Middleware.BoxOfficeMiddleware", "Create Middleware/BoxOfficeMiddleware.cs (step 3).");
        MethodInfo? invoke = boxOffice.GetMethod("InvokeAsync");
        Assert.True(invoke != null, "BoxOfficeMiddleware needs a public InvokeAsync method.");
        Assert.True(invoke!.GetParameters().Any(p => p.ParameterType.Name.StartsWith("IOptionsSnapshot")),
            "InvokeAsync should take IOptionsSnapshot<BookingRules> as a parameter (not the constructor): " +
            "middleware is created once, so scoped services go into InvokeAsync.");
    }

    // ---------- 4. Branching ----------

    [Fact]
    public async Task Module05_Step4_PingAnswersEvenWhenClosed()
    {
        foreach (bool closed in new[] { false, true })
        {
            HttpResponseMessage response = await ClientWithBoxOffice(closed).GetAsync("/ping");
            string body = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.OK && body == "pong",
                $"With BoxOfficeClosed = {closed}, /ping answered {(int)response.StatusCode} \"{body}\" instead of \"pong\". " +
                (response.StatusCode == HttpStatusCode.NotFound ? "Add app.Map(\"/ping\", ...)."
                 : response.StatusCode == HttpStatusCode.ServiceUnavailable ? "The branch must come BEFORE the box office in the pipeline."
                 : "The branch should answer exactly \"pong\"."));
        }
    }

    // ---------- 5. A filter ----------

    [Fact]
    public async Task Module05_Step5_AuditFilterSeesTheActionArguments()
    {
        Type filter = RequireType("DramaSchoolHub.Filters.AuditFilter", "Create Filters/AuditFilter.cs (step 5).");
        Assert.True(typeof(IActionFilter).IsAssignableFrom(filter), "AuditFilter should implement IActionFilter.");

        var logs = new CollectingLoggerProvider();
        HttpClient client = ClientWithLogs(logs);
        await client.GetAsync("/Home/Room/2");
        await client.GetAsync("/api/rooms");

        List<string> fromFilter = logs.From(filter);
        Assert.True(fromFilter.Any(text => text.Contains("Room") && text.Contains("id=2")),
            "After GET /Home/Room/2 the filter should log the action and its arguments, e.g. " +
            "\"Running HomeController.Room with id=2, ...\". " +
            $"It logged: [{string.Join(", ", fromFilter)}]. Is it added in AddControllersWithViews(options => options.Filters.Add<AuditFilter>())?");
        Assert.True(fromFilter.Count == 1,
            $"The filter logged {fromFilter.Count} times; expected once (for /Home/Room/2). " +
            "Filters only run for MVC actions, not for minimal API endpoints like /api/rooms.");
    }

    // ---------- helpers ----------

    public sealed class CollectingLoggerProvider : ILoggerProvider
    {
        private readonly List<(string Category, string Text)> _messages = [];

        public List<string> From(Type type)
        {
            lock (_messages) return _messages.Where(m => m.Category == type.FullName).Select(m => m.Text).ToList();
        }

        public ILogger CreateLogger(string categoryName) => new CollectingLogger(categoryName, _messages);

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
