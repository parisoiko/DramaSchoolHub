using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DramaSchoolHub.Filters;

// Logs every MVC action with its arguments, e.g. "Running HomeController.Room with id=2".
// A filter rather than middleware, because only MVC knows which action runs and with what values.
public class AuditFilter(ILogger<AuditFilter> logger) : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        // Leave out [FromServices] parameters: they're services, not values sent by the user.
        var fromServices = context.ActionDescriptor.Parameters
            .Where(parameter => parameter.BindingInfo?.BindingSource == BindingSource.Services)
            .Select(parameter => parameter.Name)
            .ToHashSet();

        string arguments = string.Join(", ", context.ActionArguments
            .Where(argument => !fromServices.Contains(argument.Key))
            .Select(argument => $"{argument.Key}={argument.Value}"));
        if (arguments == "")
        {
            arguments = "no arguments";
        }

        logger.LogInformation("Running {Action} with {Arguments}", context.ActionDescriptor.DisplayName, arguments);
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
