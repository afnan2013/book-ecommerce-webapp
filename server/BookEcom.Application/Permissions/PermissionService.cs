using BookEcom.Application.Dtos.Permissions;
using BookEcom.Domain.Abstractions;

namespace BookEcom.Application.Permissions;

public class PermissionService(
    IPermissionRepository permissionRepo,
    ILogger<PermissionService> logger) : IPermissionService
{
    public async Task<IReadOnlyList<PermissionDto>> GetAllAsync(CancellationToken ct)
    {
        var permissions = await permissionRepo.GetAllAsync(ct);

        logger.LogInformation("Permissions.GetAll — returning {Count} permissions", permissions.Count);
        return permissions.Select(p => new PermissionDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
        }).ToList();
    }
}
