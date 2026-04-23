using BookEcom.Api.Auth;
using BookEcom.Api.Data;
using BookEcom.Api.Dtos.Permissions;
using BookEcom.Api.Dtos.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookEcom.Api.Application.Users;

/// <summary>
/// Builds <see cref="UserResponse"/>s for one or many users. Extracted so
/// every user-returning endpoint projects identically (the audit found three
/// near-identical copies across UsersController — subtle divergence was only
/// a matter of time).
/// </summary>
public class UserResponseProjector(
    UserManager<AppUser> userManager,
    AppDbContext db)
{
    public async Task<UserResponse> ProjectOneAsync(AppUser user, CancellationToken ct)
    {
        var roleNames = await userManager.GetRolesAsync(user);

        var roles = await db.Roles
            .AsNoTracking()
            .Where(r => roleNames.Contains(r.Name!))
            .Select(r => new UserRoleDto { Id = r.Id, Name = r.Name! })
            .ToListAsync(ct);

        var directPermissions = await db.UserPermissions
            .AsNoTracking()
            .Where(up => up.UserId == user.Id)
            .Include(up => up.Permission)
            .Select(up => new PermissionDto
            {
                Id = up.Permission.Id,
                Name = up.Permission.Name,
                Description = up.Permission.Description,
            })
            .ToListAsync(ct);

        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email ?? "",
            FullName = user.FullName,
            UserType = user.UserType,
            ConcurrencyStamp = user.ConcurrencyStamp ?? "",
            Roles = roles,
            DirectPermissions = directPermissions,
        };
    }

    /// <summary>
    /// Bulk projection with three pre-fetched look-ups to avoid N+1 queries
    /// on list endpoints. Filtered to the input user IDs so the pattern
    /// scales when pagination lands.
    /// </summary>
    public async Task<IReadOnlyList<UserResponse>> ProjectManyAsync(
        IReadOnlyList<AppUser> users, CancellationToken ct)
    {
        if (users.Count == 0) return Array.Empty<UserResponse>();

        var userIds = users.Select(u => u.Id).ToList();

        var userRoles = await db.UserRoles
            .AsNoTracking()
            .Where(ur => userIds.Contains(ur.UserId))
            .ToListAsync(ct);

        var roles = await db.Roles.AsNoTracking().ToDictionaryAsync(r => r.Id, ct);

        var userPermissions = await db.UserPermissions
            .AsNoTracking()
            .Where(up => userIds.Contains(up.UserId))
            .Include(up => up.Permission)
            .ToListAsync(ct);

        return users.Select(u => new UserResponse
        {
            Id = u.Id,
            Email = u.Email ?? "",
            FullName = u.FullName,
            UserType = u.UserType,
            ConcurrencyStamp = u.ConcurrencyStamp ?? "",
            Roles = userRoles
                .Where(ur => ur.UserId == u.Id)
                .Select(ur => roles.TryGetValue(ur.RoleId, out var role)
                    ? new UserRoleDto { Id = role.Id, Name = role.Name! }
                    : null)
                .Where(r => r is not null)
                .ToList()!,
            DirectPermissions = userPermissions
                .Where(up => up.UserId == u.Id)
                .Select(up => new PermissionDto
                {
                    Id = up.Permission.Id,
                    Name = up.Permission.Name,
                    Description = up.Permission.Description,
                })
                .ToList(),
        }).ToList();
    }
}
