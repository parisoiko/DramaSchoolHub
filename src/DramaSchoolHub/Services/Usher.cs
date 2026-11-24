namespace DramaSchoolHub.Services;

// Checks the visitor's pass for this request. Must be scoped (or transient): as a singleton it would
// keep the first request's ScopedPass forever, and in Development the app refuses to start.
public class Usher(ScopedPass pass, ILogger<Usher> logger) : IDisposable
{
    public bool IsDisposed { get; private set; }

    public string Greet() => $"Welcome, pass {pass.Code}!";

    // Called by the container when the request ends; never call it yourself.
    public void Dispose()
    {
        IsDisposed = true;
        logger.LogInformation("Usher for pass {Code} is going home", pass.Code);
    }
}
