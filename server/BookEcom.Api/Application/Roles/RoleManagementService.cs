using BookEcom.Api.Dtos.Permissions;
using BookEcom.Api.Dtos.Roles;
using BookEcom.Domain.Auth;
using BookEcom.Domain.Common.Results;
using BookEcom.Domain.Entities;
using BookEcom.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookEcom.Api.Application.Roles;

public class RoleManagementService(
    RoleManager<IdentityRole<int>> roleManager,
    AppDbContext db,
    ILogger<RoleManagementService> logger) : IRoleManagementService
{
    public async Task<IReadOnlyList<RoleResponse>> GetAllAsync(CancellationToken ct)
    {
        // Left-join roles → role_permissions → permissions in a single query.
        // GroupBy translates more predictably on the client side here than
        // attempting a DB-side group over a navigation-less join.
        var rolePermissions = await db.Roles
            .AsNoTracking()
            .GroupJoin(
                db.RolePermissions.Include(rp => rp.Permission),
                role => role.Id,
                rp => rp.RoleId,
                (role, perms) => new RoleResponse
                {
                    Id = role.Id,
                    Name = role.Name!,
                    ConcurrencyStamp = role.ConcurrencyStamp!,
                    Permissions = perms.Select(rp => new PermissionDto
                    {
                        Id = rp.Permission.Id,
                        Name = rp.Permission.Name,
                        Description = rp.Permission.Description,
                    }).ToList(),
                })
            .ToListAsync(ct);

        logger.LogInformation("Roles.GetAll — returning {Count} roles", rolePermissions.Count);
        return rolePermissions;
    }

    public async Task<Result<RoleResponse>> GetByIdAsync(int id, CancellationToken ct)
    {
        var role = await db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);
        if (role is null) return Result<RoleResponse>.NotFound($"Role {id} not found.");

        var permissions = await db.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.RoleId == id)
            .Include(rp => rp.Permission)
            .Select(rp => new PermissionDto
            {
                Id = rp.Permission.Id,
                Name = rp.Permission.Name,
                Description = rp.Permission.Description,
            })
            .ToListAsync(ct);

        return new RoleResponse
        {
            Id = role.Id,
            Name = role.Name!,
            ConcurrencyStamp = role.ConcurrencyStamp!,
            Permissions = permissions,
        };
    }

    public async Task<Result<RoleResponse>> CreateAsync(CreateRoleRequest req, CancellationToken ct)
    {
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
        // Identity's cascade for AspNetUserRoles.
        await db.RolePermissions.Where(rp => rp.RoleId == id).ExecuteDeleteAsync(ct);

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
        // Tracked (not AsNoTracking) so EF sees the ConcurrencyStamp change.
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (role is null) return Result<RoleResponse>.NotFound($"Role {id} not found.");

        if (RoleNames.IsSuperAdmin(role.NormalizedName))
        {
            return Result<RoleResponse>.Validation(
                "SuperAdmin permissions are managed automatically and cannot be modified.");
        }

        if (role.ConcurrencyStamp != req.ConcurrencyStamp)
        {
            return Result<RoleResponse>.Conflict(
                "This role was modified by someone else. Please refresh and try again.");
        }

        var validPermissions = await db.Permissions
            .Where(p => req.PermissionIds.Contains(p.Id))
            .ToListAsync(ct);

        var invalidIds = req.PermissionIds.Except(validPermissions.Select(p => p.Id)).ToList();
        if (invalidIds.Count > 0)
        {
            return Result<RoleResponse>.Validation(
                $"Invalid permission IDs: {string.Join(", ", invalidIds)}");
        }

        // Transaction: ExecuteDeleteAsync runs outside the change tracker, so
        // wrap with the subsequent Add+SaveChanges — rollback on failure.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await db.RolePermissions.Where(rp => rp.RoleId == id).ExecuteDeleteAsync(ct);

            foreach (var perm in validPermissions)
            {
                db.RolePermissions.Add(new RolePermission
                {
                    RoleId = id,
                    PermissionId = perm.Id,
                });
            }

            role.ConcurrencyStamp = Guid.NewGuid().ToString();
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(ct);
            return Result<RoleResponse>.Conflict(
                "This role was modified by someone else. Please refresh and try again.");
        }

        logger.LogInformation(
            "Roles.SetPermissions — set {Count} permissions on {Name}",
            validPermissions.Count, role.Name);

        return new RoleResponse
        {
            Id = role.Id,
            Name = role.Name!,
            ConcurrencyStamp = role.ConcurrencyStamp,
            Permissions = validPermissions.Select(p => new PermissionDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
            }).ToList(),
        };
    }
}
