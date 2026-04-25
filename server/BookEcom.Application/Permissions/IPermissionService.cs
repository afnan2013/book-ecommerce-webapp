using BookEcom.Application.Dtos.Permissions;

namespace BookEcom.Application.Permissions;

public interface IPermissionService
{
    Task<IReadOnlyList<PermissionDto>> GetAllAsync(CancellationToken ct);

    /// <summary>
    /// Resolves the user's effective permissions = (permissions from all
    /// their roles) ∪ (their direct permissions). SuperAdmin always returns
    /// the full <see cref="BookEcom.Domain.Auth.PermissionNames.All"/> catalog,
    /// regardless of what's in the role/direct tables — the protection rule
    /// is enforced at computation time, not at write time.
    ///
    /// Called once per login by <c>AuthService</c>; the result is embedded
    /// into the JWT as <c>perm</c> claims so authorization checks become a
    /// claim read instead of a DB round-trip per request.
    /// </summary>
    Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(int userId, CancellationToken ct);
}
