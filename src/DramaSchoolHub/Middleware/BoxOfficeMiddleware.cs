using DramaSchoolHub.Models;
using Microsoft.Extensions.Options;

namespace DramaSchoolHub.Middleware;

// While BookingRules.BoxOfficeClosed is on, every request that reaches it gets a 503.
public class BoxOfficeMiddleware(RequestDelegate next)
{
    // Middleware is created once, so scoped services such as IOptionsSnapshot go here, not in the
    // constructor. A snapshot is re-read per request, so the box office opens and closes without a restart.
    public async Task InvokeAsync(HttpContext context, IOptionsSnapshot<BookingRules> rules)
    {
        if (rules.Value.BoxOfficeClosed)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("The box office is closed. Please come back later.");
            return;
        }

        await next(context);
    }
}
