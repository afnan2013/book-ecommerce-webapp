using BookEcom.Api.Dtos.Auth;
using BookEcom.Domain.Common.Results;

namespace BookEcom.Api.Application.Auth;

public interface IAuthService
{
    Task<Result<LoginResponse>> RegisterAsync(RegisterRequest req, CancellationToken ct);
    Task<Result<LoginResponse>> LoginAsync(LoginRequest req, CancellationToken ct);
}
