namespace BookEcom.Application.Auth.Jwt;

/// <summary>
/// Issues access tokens for an authenticated user. Application-owned
/// abstraction; the concrete implementation lives in Infrastructure where
/// the JWT library and signing-key handling belong.
/// </summary>
public interface IJwtTokenService
{
    (string token, DateTime expiresAt) CreateAccessToken(AppUser user);
}
