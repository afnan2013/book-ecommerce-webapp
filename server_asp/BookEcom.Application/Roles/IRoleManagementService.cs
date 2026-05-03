using BookEcom.Application.Dtos.Roles;
using BookEcom.Domain.Common.Results;

namespace BookEcom.Application.Roles;

public interface IRoleManagementService
{
    Task<IReadOnlyList<RoleResponse>> GetAllAsync(CancellationToken ct);
    Task<Result<RoleResponse>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<RoleResponse>> CreateAsync(CreateRoleRequest req, CancellationToken ct);
    Task<Result> UpdateAsync(int id, UpdateRoleRequest req, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
    Task<Result<RoleResponse>> SetPermissionsAsync(int id, SetRolePermissionsRequest req, CancellationToken ct);
}
