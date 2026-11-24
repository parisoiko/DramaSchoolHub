using DramaSchoolHub.Models;
using Microsoft.Extensions.Options;

namespace DramaSchoolHub.Middleware;

public class BoxOfficeMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IOptionsSnapshot<BookingRules> rules)
    {
        if (rules.Value.BoxOfficeClosed)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("The box office is closed. Please come back later.");
            return;                       // stop here: don't call next
        }

        await next(context);
    }
}