using BookEcom.Domain.Common.Errors;
using Microsoft.AspNetCore.Mvc;

namespace BookEcom.Domain.Common.Results;

/// <summary>
/// Translates domain <see cref="Result"/> / <see cref="Result{T}"/> outcomes
/// into ASP.NET <see cref="ActionResult"/>s. This is the single seam between
/// the Application layer (HTTP-ignorant) and the transport layer — every
/// controller goes through here, so the wire format is one-file-controlled.
///
/// Failure responses follow RFC 7807 (<c>application/problem+json</c>):
/// every non-success Result becomes a <see cref="ProblemDetails"/> body
/// with <c>type</c>, <c>title</c>, <c>status</c>, <c>detail</c>. Domain
/// validation errors that carry multiple detail strings (e.g. an Identity
/// error list) attach them as a top-level <c>errors</c> extension so the
/// client can present them as a list. The shape matches what
/// <c>AddProblemDetails()</c> produces for framework-side errors (model
/// binding 400s, auth 401/403), so clients see one consistent error
/// envelope regardless of where the failure originated.
/// </summary>
public static class ResultExtensions
{
    public static ActionResult ToActionResult(this Result result) =>
        result.IsSuccess ? new NoContentResult() : ToFailureResult(result.Error!);

    public static ActionResult<T> ToActionResult<T>(this Result<T> result) =>
        result.IsSuccess ? new OkObjectResult(result.Value) : ToFailureResult(result.Error!);

    /// <summary>
    /// Success → 201 Created with a <c>Location</c> header pointing at the
    /// given GET action. Failure → routed through the shared
    /// <see cref="ToActionResult{T}(Result{T})"/> mapper so error shapes stay
    /// identical across every endpoint. Use this for POSTs that create a
    /// new resource (REST convention: 201 + Location, not 200).
    /// </summary>
    public static ActionResult<T> ToCreatedAtAction<T>(
        this Result<T> result,
        ControllerBase controller,
        string actionName,
        Func<T, object> routeValues)
    {
        if (result.IsFailure) return result.ToActionResult();
        var value = result.Value!;
        return controller.CreatedAtAction(actionName, routeValues(value), value);
    }

    private static ActionResult ToFailureResult(Error error)
    {
        var (status, title) = error.Code switch
        {
            ErrorCode.NotFound     => (StatusCodes.Status404NotFound,            "Not Found"),
            ErrorCode.Conflict     => (StatusCodes.Status409Conflict,            "Conflict"),
            ErrorCode.Validation   => (StatusCodes.Status400BadRequest,          "Validation Failed"),
            ErrorCode.Forbidden    => (StatusCodes.Status403Forbidden,           "Forbidden"),
            ErrorCode.Unauthorized => (StatusCodes.Status401Unauthorized,        "Unauthorized"),
            ErrorCode.Unexpected   => (StatusCodes.Status500InternalServerError, "Server Error"),
            _                      => (StatusCodes.Status500InternalServerError, "Server Error"),
        };

        var problem = new ProblemDetails
        {
            Type = $"https://httpstatuses.io/{status}",
            Title = title,
            Status = status,
            Detail = error.Message,
        };

        if (error.Details is not null)
        {
            // Custom extension. Our domain Validation errors aren't
            // field-keyed (they're business-rule strings + Identity
            // error lists), so we don't shape this as
            // ValidationProblemDetails.errors[field]. The client's
            // ApiError parser tolerates either shape via `errors?:
            // unknown`.
            problem.Extensions["errors"] = error.Details;
        }

        return new ObjectResult(problem)
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" },
        };
    }
}
