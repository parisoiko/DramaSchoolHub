using Microsoft.AspNetCore.Mvc.Filters;

namespace DramaSchoolHub.Filters;

public class AuditFilter(ILogger<AuditFilter> logger) : IActionFilter
{
    // Runs just BEFORE the action: the arguments are already filled in.
    public void OnActionExecuting(ActionExecutingContext context)
    {
        string arguments = string.Join(", ", context.ActionArguments.Select(a => $"{a.Key}={a.Value}"));
        logger.LogInformation("Running {Action} with {Arguments}", context.ActionDescriptor.DisplayName, arguments);
    }

    // Runs just AFTER the action. Nothing to do here, but the interface requires it.
    public void OnActionExecuted(ActionExecutedContext context) { }
}