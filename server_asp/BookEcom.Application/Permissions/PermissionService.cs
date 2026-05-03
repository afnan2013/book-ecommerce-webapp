using BookEcom.Application.Dtos.Permissions;
using BookEcom.Domain.Abstractions;
using BookEcom.Domain.Auth;

namespace BookEcom.Application.Permissions;

public class PermissionService(
    IPermissionRepository permissionRepo,
    IUserRepository userRepo,
    IRoleRepository roleRepo,
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

    public async Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(
        int userId, CancellationToken ct)
    {
        var roles = await userRepo.GetRolesForUserAsync(userId, ct);

        // SuperAdmin shortcut. The seeder reconciles the SuperAdmin role's
        // role_permissions on every boot, but emitting PermissionNames.All
        // directly is shorter, faster, and survives a transient mismatch
        // between code and DB during a partial deploy.
        if (roles.Any(r => RoleNames.IsSuperAdmin(r.NormalizedName)))
            return PermissionNames.All.ToList();

        var directPermissions = await userRepo.GetDirectPermissionsForUserAsync(userId, ct);

        if (roles.Count == 0)
            return directPermissions.Select(p => p.Name).Distinct().ToList();

        var rolePermissionsByRole = await roleRepo.GetPermissionsForRolesAsync(
            roles.Select(r => r.Id), ct);

        return rolePermissionsByRole.Values
            .SelectMany(p => p)
            .Select(p => p.Name)
            .Concat(directPermissions.Select(p => p.Name))
            .Distinct()
            .ToList();
    }
}
