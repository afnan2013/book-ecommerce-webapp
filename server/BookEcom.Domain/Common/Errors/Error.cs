namespace BookEcom.Domain.Common.Errors;

public enum ErrorCode
{
    NotFound,
    Conflict,
    Validation,
    Forbidden,
    Unauthorized,
    Unexpected,
}

public sealed record Error(ErrorCode Code, string Message, IReadOnlyList<string>? Details = null);
