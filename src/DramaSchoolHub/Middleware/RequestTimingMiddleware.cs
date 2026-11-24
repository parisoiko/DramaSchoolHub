using System.Diagnostics;

namespace DramaSchoolHub.Middleware;

public class RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();          // on the way in: start the clock
        await next(context);                              // the whole rest of the pipeline runs here
        stopwatch.Stop();                              // on the way out: stop it
        logger.LogInformation("{Method} {Path} took {Elapsed} ms",
            context.Request.Method, context.Request.Path, stopwatch.ElapsedMilliseconds);
    }
}