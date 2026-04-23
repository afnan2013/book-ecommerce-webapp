using BookEcom.Domain.Common.Errors;
using Microsoft.AspNetCore.Mvc;

namespace BookEcom.Domain.Common.Results;

/// <summary>
/// Translates domain <see cref="Result"/> / <see cref="Result{T}"/> outcomes
/// into ASP.NET <see cref="ActionResult"/>s. This is the single seam between
/// the Application layer (HTTP-ignorant) and the transport layer — every
/// controller goes through here, so changing the wire format (e.g. Step 6's
/// ProblemDetails rollout) is a one-file edit.
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
        // Wire shape convention (preserved from pre-refactor controllers):
        //   Details present → { errors: [...] }   — e.g. Identity error lists
        //   Details absent  → { error: "..." }    — single-message failures
        // Step 6 (ProblemDetails) replaces this block with a single RFC 7807
        // mapping; callers do not change.
        object body = error.Details is not null
            ? new { errors = error.Details }
            : new { error = error.Message };

        return error.Code switch
        {
            ErrorCode.NotFound => new NotFoundObjectResult(body),
            ErrorCode.Conflict => new ConflictObjectResult(body),
            ErrorCode.Validation => new BadRequestObjectResult(body),
            ErrorCode.Forbidden => new ObjectResult(body) { StatusCode = StatusCodes.Status403Forbidden },
            ErrorCode.Unauthorized => new UnauthorizedResult(),
            ErrorCode.Unexpected => new ObjectResult(body) { StatusCode = StatusCodes.Status500InternalServerError },
            _ => new ObjectResult(body) { StatusCode = StatusCodes.Status500InternalServerError },
        };
    }
}
