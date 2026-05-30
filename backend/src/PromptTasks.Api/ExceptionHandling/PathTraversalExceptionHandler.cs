using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PromptTasks.Application.Common.Exceptions;

namespace PromptTasks.Api.ExceptionHandling;

public sealed class PathTraversalExceptionHandler(ILogger<PathTraversalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not PathTraversalException)
        {
            return false;
        }

        logger.LogWarning(exception, "Rejected unsafe path");

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid path",
            Detail = exception.Message,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
        };

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
