using BookEcom.Application.Dtos.Permissions;
using BookEcom.Application.Dtos.Users;
using BookEcom.Domain.Abstractions;

namespace BookEcom.Application.Users;

/// <summary>
/// Builds <see cref="UserResponse"/>s for one or many users. Extracted so
/// every user-returning endpoint projects identically (the audit found three
/// near-identical copies across UsersController — subtle divergence was only
/// a matter of time).
/// </summary>
public class UserResponseProjector(IUserRepository userRepo)
{
    public async Task<UserResponse> ProjectOneAsync(UserSnapshot user, CancellationToken ct)
    {
        var roles = await userRepo.GetRolesForUserAsync(user.Id, ct);
        var directPermissions = await userRepo.GetDirectPermissionsForUserAsync(user.Id, ct);

        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            UserType = user.UserType,
            ConcurrencyStamp = user.ConcurrencyStamp,
            Roles = roles
                .Select(r => new UserRoleDto { Id = r.Id, Name = r.Name })
                .ToList(),
            DirectPermissions = directPermissions
                .Select(p => new PermissionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                })
                .ToList(),
        };
    }

    /// <summary>
    /// Bulk projection with two pre-fetched dictionaries to avoid N+1 queries
    /// on list endpoints. Filtered to the input user IDs so the pattern
    /// scales when pagination lands.
    /// </summary>
    public async Task<IReadOnlyList<UserResponse>> ProjectManyAsync(
        IReadOnlyList<UserSnapshot> users, CancellationToken ct)
    {
        if (users.Count == 0) return Array.Empty<UserResponse>();

        var userIds = users.Select(u => u.Id).ToList();
        var rolesByUser = await userRepo.GetRolesForUsersAsync(userIds, ct);
        var permsByUser = await userRepo.GetDirectPermissionsForUsersAsync(userIds, ct);

        return users.Select(u => new UserResponse
        {
            Id = u.Id,
            Email = u.Email,
            FullName = u.FullName,
            UserType = u.UserType,
            ConcurrencyStamp = u.ConcurrencyStamp,
            Roles = (rolesByUser.TryGetValue(u.Id, out var rs) ? rs : [])
                .Select(r => new UserRoleDto { Id = r.Id, Name = r.Name })
                .ToList(),
            DirectPermissions = (permsByUser.TryGetValue(u.Id, out var ps) ? ps : [])
                .Select(p => new PermissionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                })
                .ToList(),
        }).ToList();
    }
}
