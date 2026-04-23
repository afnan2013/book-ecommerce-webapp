using BookEcom.Application.Dtos.Auth;
using BookEcom.Application.Dtos.Users;
using BookEcom.Domain.Common.Results;

namespace BookEcom.Application.Users;

public interface IUserManagementService
{
    Task<Result<UserDto>> GetMeAsync(int callerId, CancellationToken ct);
    Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken ct);
    Task<Result<UserResponse>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<UserResponse>> CreateAsync(CreateUserRequest req, CancellationToken ct);
    Task<Result> DeleteAsync(int id, int callerId, CancellationToken ct);
    Task<Result<UserResponse>> SetRolesAsync(int id, SetUserRolesRequest req, CancellationToken ct);
    Task<Result<UserResponse>> SetPermissionsAsync(int id, SetUserPermissionsRequest req, CancellationToken ct);
}
