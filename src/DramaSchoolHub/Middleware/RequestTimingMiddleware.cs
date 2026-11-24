using System.Diagnostics;

namespace DramaSchoolHub.Middleware;

// Logs how long each request took, e.g. "GET /rooms/2 took 14 ms".
public class RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        await next(context);
        stopwatch.Stop();

        logger.LogInformation("{Method} {Path} took {Elapsed} ms",
            context.Request.Method, context.Request.Path, stopwatch.ElapsedMilliseconds);
    }
}
