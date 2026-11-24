namespace DramaSchoolHub.Services;

public class Usher(ScopedPass pass, ILogger<Usher> logger) : IDisposable
{
    public bool IsDisposed { get; private set; }

    public string Greet() => $"Welcome, pass {pass.Code}!";

    public void Dispose()
    {
        IsDisposed = true;
        logger.LogInformation("Usher for pass {Code} is going home", pass.Code);
    }
}
