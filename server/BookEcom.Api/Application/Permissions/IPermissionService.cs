using BookEcom.Api.Dtos.Permissions;

namespace BookEcom.Api.Application.Permissions;

public interface IPermissionService
{
    Task<IReadOnlyList<PermissionDto>> GetAllAsync(CancellationToken ct);
}
