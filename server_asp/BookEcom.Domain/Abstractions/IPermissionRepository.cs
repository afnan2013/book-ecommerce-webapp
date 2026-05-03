using BookEcom.Domain.Entities;

namespace BookEcom.Domain.Abstractions;

/// <summary>
/// Persistence contract for the <see cref="Permission"/> aggregate. Permissions
/// are a code-seeded catalog — no mutation surface here; admins only read them.
/// <see cref="GetByIdsAsync"/> exists for cross-aggregate validation (Role and
/// User repos need to verify permission IDs a caller supplied actually exist).
/// </summary>
public interface IPermissionRepository
{
    Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken ct);

    Task<IReadOnlyList<Permission>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct);
}
