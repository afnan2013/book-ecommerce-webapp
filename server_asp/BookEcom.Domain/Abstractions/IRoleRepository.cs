using BookEcom.Domain.Entities;

namespace BookEcom.Domain.Abstractions;

/// <summary>
/// Persistence contract for the Role aggregate and its <see cref="RolePermission"/>
/// child rows. Role identity (Create / Update / Delete of the role itself) is
/// owned by ASP.NET Identity's <c>RoleManager</c> — this repo only handles the
/// permission-attachment surface plus read projections. Reads return
/// <see cref="RoleSummary"/> rather than <c>IdentityRole</c> so Domain stays
/// framework-agnostic.
/// </summary>
public interface IRoleRepository
{
    Task<IReadOnlyList<RoleSummary>> GetAllAsync(CancellationToken ct);

    Task<RoleSummary?> GetByIdAsync(int id, CancellationToken ct);

    /// <summary>
    /// Bulk fetch of roles by id. Used by user-role assignment flows that
    /// need to validate caller-supplied role ids exist before mutating.
    /// </summary>
    Task<IReadOnlyList<RoleSummary>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct);

    Task<IReadOnlyList<Permission>> GetPermissionsForRoleAsync(int roleId, CancellationToken ct);

    /// <summary>
    /// Bulk load permissions for a set of roles in a single query, keyed by
    /// <c>RoleId</c>. Roles with no permissions are absent from the dictionary
    /// (callers should default to an empty list). This exists to avoid an N+1
    /// on the GetAll endpoint.
    /// </summary>
    Task<IReadOnlyDictionary<int, IReadOnlyList<Permission>>> GetPermissionsForRolesAsync(
        IEnumerable<int> roleIds, CancellationToken ct);

    Task ClearPermissionsAsync(int roleId, CancellationToken ct);

    void AddRolePermission(RolePermission rolePermission);

    /// <summary>
    /// Atomically set the role's ConcurrencyStamp to <paramref name="newStamp"/>
    /// iff the stored stamp still equals <paramref name="expectedStamp"/>.
    /// Returns <c>true</c> if the row was updated, <c>false</c> if someone
    /// else modified it first (caller should return 409). Implemented via
    /// <c>ExecuteUpdateAsync</c> so it bypasses the change tracker and
    /// commits as a single SQL statement inside the ambient transaction.
    /// </summary>
    Task<bool> UpdateConcurrencyStampAsync(
        int roleId, string expectedStamp, string newStamp, CancellationToken ct);
}
