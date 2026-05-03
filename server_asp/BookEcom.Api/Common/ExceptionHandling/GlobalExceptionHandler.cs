using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BookEcom.Api.Common.ExceptionHandling;

/// <summary>
/// Catches anything that escapes a controller without being mapped to a
/// <see cref="Microsoft.AspNetCore.Http.IResult"/> or <see cref="ActionResult"/>
/// and renders an RFC 7807 ProblemDetails 500 response. Logs the exception
/// with the request method + path so server-side diagnostics still work
/// after the response is sanitised for the client.
///
/// Expected outcomes (Result-based domain failures) flow through
/// <c>ResultExtensions.ToFailureResult</c> instead — this handler is only
/// the safety net for unhandled exceptions (DB outage, null deref, etc.).
/// In Development we leak exception type + stack trace as ProblemDetails
/// extensions for fast iteration; in Production those are stripped.
/// </summary>
public class GlobalExceptionHandler(
    IHostEnvironment env,
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken ct)
    {
        logger.LogError(exception,
            "Unhandled exception during {Method} {Path}",
            context.Request.Method, context.Request.Path);

        var problem = new ProblemDetails
        {
            Type = "https://httpstatuses.io/500",
            Title = "Server Error",
            Status = StatusCodes.Status500InternalServerError,
            Detail = env.IsDevelopment()
                ? exception.Message
                : "An unexpected error occurred. Please try again or contact support if the problem persists.",
        };

        if (env.IsDevelopment())
        {
            problem.Extensions["exceptionType"] = exception.GetType().FullName;
            problem.Extensions["stackTrace"] = exception.StackTrace;
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem,
        });
    }
}
