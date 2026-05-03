using BookEcom.Domain.Entities;

namespace BookEcom.Domain.Abstractions;

/// <summary>
/// Persistence contract for the User aggregate plus its <see cref="UserPermission"/>
/// child rows. User identity (Create / Update / Delete of the user itself,
/// password hashing, role assignment) is owned by ASP.NET Identity's
/// <c>UserManager</c> — this repo handles direct-permission management,
/// raw role lookups for the projector, and read projections that the
/// Application layer composes into <c>UserResponse</c> DTOs.
/// </summary>
public interface IUserRepository
{
    Task<UserSnapshot?> GetByIdAsync(int id, CancellationToken ct);

    Task<IReadOnlyList<UserSnapshot>> GetAllAsync(CancellationToken ct);

    /// <summary>
    /// Roles assigned to a single user, joined through <c>AspNetUserRoles</c>.
    /// Returns <see cref="RoleSummary"/> rather than role names so callers
    /// don't need a second round-trip to resolve names → ids.
    /// </summary>
    Task<IReadOnlyList<RoleSummary>> GetRolesForUserAsync(int userId, CancellationToken ct);

    /// <summary>
    /// Bulk variant for the list-projection N+1 fix. Roles with no users
    /// in the input set are omitted; users with no roles are omitted.
    /// </summary>
    Task<IReadOnlyDictionary<int, IReadOnlyList<RoleSummary>>> GetRolesForUsersAsync(
        IEnumerable<int> userIds, CancellationToken ct);

    Task<IReadOnlyList<Permission>> GetDirectPermissionsForUserAsync(int userId, CancellationToken ct);

    Task<IReadOnlyDictionary<int, IReadOnlyList<Permission>>> GetDirectPermissionsForUsersAsync(
        IEnumerable<int> userIds, CancellationToken ct);

    Task ClearDirectPermissionsAsync(int userId, CancellationToken ct);

    void AddUserPermission(UserPermission userPermission);

    /// <summary>
    /// Atomic concurrency-checked stamp bump. Same pattern as
    /// <c>IRoleRepository.UpdateConcurrencyStampAsync</c>: returns
    /// <c>true</c> iff the stored stamp matched <paramref name="expectedStamp"/>.
    /// </summary>
    Task<bool> UpdateConcurrencyStampAsync(
        int userId, string expectedStamp, string newStamp, CancellationToken ct);

    /// <summary>
    /// How many users are currently assigned a given role. Used by
    /// <c>LastSuperAdminPolicy</c> to refuse the "delete the last SuperAdmin"
    /// operation. Lives on user repo because the row count is over users,
    /// not roles.
    /// </summary>
    Task<int> CountUsersInRoleAsync(int roleId, CancellationToken ct);
}
