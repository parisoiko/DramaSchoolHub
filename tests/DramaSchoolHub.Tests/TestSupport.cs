using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DramaSchoolHub.Tests;

public static class FactoryExtensions
{
    // Starts a copy of the app with some settings overridden, e.g. ["BookingRules:BoxOfficeClosed"] = "true".
    public static WebApplicationFactory<Program> WithSettings(
        this WebApplicationFactory<Program> factory, Dictionary<string, string?> settings) =>
        factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(settings)));

    // Starts a copy of the app whose log messages end up in `logs`.
    public static WebApplicationFactory<Program> WithLogs(
        this WebApplicationFactory<Program> factory, CollectingLoggerProvider logs) =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddSingleton<ILoggerProvider>(logs)));

    // The message of the error that stopped the app from starting, or "" if it started.
    public static string StartupError(this WebApplicationFactory<Program> factory)
    {
        try
        {
            using HttpClient _ = factory.CreateClient();
            return "";
        }
        catch (Exception e)
        {
            return e.GetBaseException().Message;
        }
    }
}

// Collects log messages in memory so tests can check what was logged.
public sealed class CollectingLoggerProvider : ILoggerProvider
{
    private readonly List<(string Category, string Text)> _messages = [];

    public List<string> From<T>()
    {
        lock (_messages)
        {
            return _messages.Where(m => m.Category == typeof(T).FullName).Select(m => m.Text).ToList();
        }
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
            lock (messages)
            {
                messages.Add((category, formatter(state, exception)));
            }
        }
    }
}
