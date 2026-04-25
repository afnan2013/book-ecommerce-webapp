namespace BookEcom.Application.Auth.Jwt;

/// <summary>
/// Issues access tokens for an authenticated user. Application-owned
/// abstraction; the concrete implementation lives in Infrastructure where
/// the JWT library and signing-key handling belong.
///
/// <paramref name="permissions"/> is emitted as one <c>perm</c> claim per
/// entry so the authorization handler can verify gates with a single claim
/// read — no DB round-trip per request. Caller computes the effective set
/// (typically via <c>IPermissionService.GetEffectivePermissionsAsync</c>);
/// the token service is intentionally dumb about RBAC semantics.
/// </summary>
public interface IJwtTokenService
{
    (string token, DateTime expiresAt) CreateAccessToken(
        AppUser user, IEnumerable<string> permissions);
}
