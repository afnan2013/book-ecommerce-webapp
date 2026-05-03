using BookEcom.Domain.Common.Errors;

namespace BookEcom.Domain.Common.Results;

/// <summary>
/// Outcome of an operation that does not produce a value. Expected failures
/// (not-found, conflict, validation, forbidden) travel through
/// <see cref="Error"/>; unexpected exceptions keep flowing up to middleware.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; }

    protected Result(bool isSuccess, Error? error)
    {
        if (isSuccess && error is not null)
            throw new InvalidOperationException("A successful result cannot carry an error.");
        if (!isSuccess && error is null)
            throw new InvalidOperationException("A failed result must carry an error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(Error error) => new(false, error);

    public static Result NotFound(string message) =>
        Failure(new Error(ErrorCode.NotFound, message));

    public static Result Conflict(string message) =>
        Failure(new Error(ErrorCode.Conflict, message));

    public static Result Validation(string message, IReadOnlyList<string>? details = null) =>
        Failure(new Error(ErrorCode.Validation, message, details));

    public static Result Forbidden(string message) =>
        Failure(new Error(ErrorCode.Forbidden, message));

    public static Result Unauthorized(string message) =>
        Failure(new Error(ErrorCode.Unauthorized, message));

    public static Result Unexpected(string message) =>
        Failure(new Error(ErrorCode.Unexpected, message));

    public TOut Match<TOut>(Func<TOut> onSuccess, Func<Error, TOut> onFailure) =>
        IsSuccess ? onSuccess() : onFailure(Error!);

    public static implicit operator Result(Error error) => Failure(error);
}
