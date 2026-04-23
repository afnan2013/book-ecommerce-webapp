namespace BookEcom.Api.Common.Errors;

public enum ErrorCode
{
    NotFound,
    Conflict,
    Validation,
    Forbidden,
    Unexpected,
}

public sealed record Error(ErrorCode Code, string Message, IReadOnlyList<string>? Details = null);
