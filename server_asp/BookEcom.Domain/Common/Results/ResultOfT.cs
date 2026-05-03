using BookEcom.Domain.Common.Errors;

namespace BookEcom.Domain.Common.Results;

/// <summary>
/// Outcome of an operation that produces a value on success.
/// <see cref="Value"/> is non-null iff <see cref="Result.IsSuccess"/>.
/// </summary>
public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(T value) : base(true, null)
    {
        Value = value;
    }

    private Result(Error error) : base(false, error)
    {
    }

    public static Result<T> Success(T value) => new(value);
    public new static Result<T> Failure(Error error) => new(error);

    public new static Result<T> NotFound(string message) =>
        new(new Error(ErrorCode.NotFound, message));

    public new static Result<T> Conflict(string message) =>
        new(new Error(ErrorCode.Conflict, message));

    public new static Result<T> Validation(string message, IReadOnlyList<string>? details = null) =>
        new(new Error(ErrorCode.Validation, message, details));

    public new static Result<T> Forbidden(string message) =>
        new(new Error(ErrorCode.Forbidden, message));

    public new static Result<T> Unauthorized(string message) =>
        new(new Error(ErrorCode.Unauthorized, message));

    public new static Result<T> Unexpected(string message) =>
        new(new Error(ErrorCode.Unexpected, message));

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure) =>
        IsSuccess ? onSuccess(Value!) : onFailure(Error!);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}
