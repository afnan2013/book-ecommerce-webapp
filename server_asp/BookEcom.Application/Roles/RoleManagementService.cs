using BookEcom.Application.Dtos.Permissions;
using BookEcom.Application.Dtos.Roles;
using BookEcom.Domain.Abstractions;
using BookEcom.Domain.Auth;
using BookEcom.Domain.Common.Results;
using BookEcom.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace BookEcom.Application.Roles;

public class RoleManagementService(
    RoleManager<IdentityRole<int>> roleManager,
    IRoleRepository roleRepo,
    IPermissionRepository permissionRepo,
    IUnitOfWork uow,
    ILogger<RoleManagementService> logger) : IRoleManagementService
{
    public async Task<IReadOnlyList<RoleResponse>> GetAllAsync(CancellationToken ct)
    {
        var roles = await roleRepo.GetAllAsync(ct);
        var permsByRole = await roleRepo.GetPermissionsForRolesAsync(
            roles.Select(r => r.Id), ct);

        logger.LogInformation("Roles.GetAll — returning {Count} roles", roles.Count);

        return roles.Select(r => new RoleResponse
        {
            Id = r.Id,
            Name = r.Name,
            ConcurrencyStamp = r.ConcurrencyStamp,
            Permissions = (permsByRole.TryGetValue(r.Id, out var perms) ? perms : [])
                .Select(p => new PermissionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                }).ToList(),
        }).ToList();
    }

    public async Task<Result<RoleResponse>> GetByIdAsync(int id, CancellationToken ct)
    {
        var role = await roleRepo.GetByIdAsync(id, ct);
        if (role is null) return Result<RoleResponse>.NotFound($"Role {id} not found.");

        var permissions = await roleRepo.GetPermissionsForRoleAsync(id, ct);

        return new RoleResponse
        {
            Id = role.Id,
            Name = role.Name,
            ConcurrencyStamp = role.ConcurrencyStamp,
            Permissions = permissions.Select(p => new PermissionDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
            }).ToList(),
        };
    }

    public async Task<Result<RoleResponse>> CreateAsync(CreateRoleRequest req, CancellationToken ct)
    {
        // RoleManager owns creation — it normalizes the name, seeds the
        // ConcurrencyStamp, and runs Identity's validators.
        var role = new IdentityRole<int> { Name = req.Name };
        var created = await roleManager.CreateAsync(role);
        if (!created.Succeeded)
        {
            return Result<RoleResponse>.Validation(
                "Could not create role.",
                created.Errors.Select(e => e.Description).ToList());
        }

        logger.LogInformation("Roles.Create — created role {Id} ({Name})", role.Id, role.Name);
        return new RoleResponse
        {
            Id = role.Id,
            Name = role.Name!,
            ConcurrencyStamp = role.ConcurrencyStamp!,
            Permissions = [],
        };
    }

    public async Task<Result> UpdateAsync(int id, UpdateRoleRequest req, CancellationToken ct)
    {
        var role = await roleManager.FindByIdAsync(id.ToString());
        if (role is null) return Result.NotFound($"Role {id} not found.");

        if (RoleNames.IsSuperAdmin(role.NormalizedName))
            return Result.Validation("SuperAdmin role cannot be modified.");

        role.Name = req.Name;
        var updated = await roleManager.UpdateAsync(role);
        if (!updated.Succeeded)
        {
            return Result.Validation(
                "Could not update role.",
                updated.Errors.Select(e => e.Description).ToList());
        }

        logger.LogInformation("Roles.Update — renamed {Id} to {Name}", id, req.Name);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var role = await roleManager.FindByIdAsync(id.ToString());
        if (role is null) return Result.NotFound($"Role {id} not found.");

        if (RoleNames.IsSuperAdmin(role.NormalizedName))
            return Result.Validation("SuperAdmin role cannot be deleted.");

        // Remove permission links first, then the role itself. Relies on
        // Identity's cascade for AspNetUserRoles. No explicit transaction —
        // matches the pre-refactor behaviour.
        await roleRepo.ClearPermissionsAsync(id, ct);

        var deleted = await roleManager.DeleteAsync(role);
        if (!deleted.Succeeded)
        {
            return Result.Validation(
                "Could not delete role.",
                deleted.Errors.Select(e => e.Description).ToList());
        }

        logger.LogInformation("Roles.Delete — deleted {Id} ({Name})", id, role.Name);
        return Result.Success();
    }

    public async Task<Result<RoleResponse>> SetPermissionsAsync(
        int id, SetRolePermissionsRequest req, CancellationToken ct)
    {
        var role = await roleRepo.GetByIdAsync(id, ct);
        if (role is null) return Result<RoleResponse>.NotFound($"Role {id} not found.");

        if (RoleNames.IsSuperAdmin(role.NormalizedName))
        {
            return Result<RoleResponse>.Validation(
                "SuperAdmin permissions are managed automatically and cannot be modified.");
        }

        // Fast-path concurrency check. The authoritative check is below
        // inside UpdateConcurrencyStampAsync — this one just avoids doing
        // any DB writes if we already know we're stale.
        if (role.ConcurrencyStamp != req.ConcurrencyStamp)
        {
            return Result<RoleResponse>.Conflict(
                "This role was modified by someone else. Please refresh and try again.");
        }

        var validPermissions = await permissionRepo.GetByIdsAsync(req.PermissionIds, ct);
        var invalidIds = req.PermissionIds.Except(validPermissions.Select(p => p.Id)).ToList();
        if (invalidIds.Count > 0)
        {
            return Result<RoleResponse>.Validation(
                $"Invalid permission IDs: {string.Join(", ", invalidIds)}");
        }

        var newStamp = Guid.NewGuid().ToString();
        await using var transaction = await uow.BeginTransactionAsync(ct);

        // ExecuteDelete participates in the ambient transaction; the subsequent
        // Adds are tracked and flushed by SaveChangesAsync into the same
        // transaction. Any exception here causes the `await using` dispose to
        // roll back.
        await roleRepo.ClearPermissionsAsync(id, ct);

        foreach (var perm in validPermissions)
        {
            roleRepo.AddRolePermission(new RolePermission
            {
                RoleId = id,
                PermissionId = perm.Id,
            });
        }

        await uow.SaveChangesAsync(ct);

        // Atomic check-and-set. If someone else mutated the role between our
        // read and this write, no rows match and we return 409.
        var stamped = await roleRepo.UpdateConcurrencyStampAsync(
            id, req.ConcurrencyStamp, newStamp, ct);
        if (!stamped)
        {
            await transaction.RollbackAsync(ct);
            return Result<RoleResponse>.Conflict(
                "This role was modified by someone else. Please refresh and try again.");
        }

        await transaction.CommitAsync(ct);

        logger.LogInformation(
            "Roles.SetPermissions — set {Count} permissions on {Name}",
            validPermissions.Count, role.Name);

        return new RoleResponse
        {
            Id = role.Id,
            Name = role.Name,
            ConcurrencyStamp = newStamp,
            Permissions = validPermissions.Select(p => new PermissionDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
            }).ToList(),
        };
    }
}
