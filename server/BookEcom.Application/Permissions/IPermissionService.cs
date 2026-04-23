using BookEcom.Application.Dtos.Permissions;

namespace BookEcom.Application.Permissions;

public interface IPermissionService
{
    Task<IReadOnlyList<PermissionDto>> GetAllAsync(CancellationToken ct);
}
