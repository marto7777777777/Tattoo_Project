using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Tattoo_Project.Services;

namespace Tattoo_Project.Middleware;

public sealed class ApiExceptionHandler(
    ILogger<ApiExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is DbUpdateConcurrencyException)
        {
            logger.LogInformation("EF Core concurrency conflict. TraceId: {TraceId}", httpContext.TraceIdentifier);
            httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Request conflict",
                    Detail = "The resource changed while this request was being processed. Refresh and try again.",
                    Extensions =
                    {
                        ["code"] = "resource_concurrency_conflict",
                        ["correlationId"] = httpContext.TraceIdentifier
                    }
                }
            });
        }

        if (exception is DomainConflictException conflict)
        {
            logger.LogInformation(exception, "Controlled domain conflict. TraceId: {TraceId}", httpContext.TraceIdentifier);
            httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Request conflict",
                    Detail = conflict.Message,
                    Extensions =
                    {
                        ["code"] = conflict.Code,
                        ["correlationId"] = httpContext.TraceIdentifier
                    }
                }
            });
        }

        logger.LogError(exception, "Unhandled request exception. TraceId: {TraceId}", httpContext.TraceIdentifier);
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Unexpected server error",
                Detail = "The request could not be completed.",
                Extensions =
                {
                    ["code"] = "unexpected_server_error",
                    ["correlationId"] = httpContext.TraceIdentifier
                }
            }
        });
    }
}
