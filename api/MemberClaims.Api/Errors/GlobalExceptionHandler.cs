using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MemberClaims.Api.Errors;

/// <summary>
/// One place that turns exceptions into consistent ProblemDetails responses.
/// Known business errors become 4xx; anything unexpected is logged and becomes a 500
/// without leaking stack traces to the client.
/// </summary>
public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            BusinessRuleException => (StatusCodes.Status422UnprocessableEntity, "Business rule violated"),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "The record was changed by someone else"),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error")
        };

        if (status >= 500)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning("Request {Method} {Path} failed with {Status}: {Message}",
                httpContext.Request.Method, httpContext.Request.Path, status, exception.Message);
        }

        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = status >= 500 ? "An unexpected error occurred. Please try again later." : exception.Message,
                Instance = httpContext.Request.Path
            }
        });
    }
}
