using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BookEcom.Application.Auth;
using BookEcom.Application.Auth.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BookEcom.Infrastructure.Auth.Jwt;

public class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    private readonly JwtOptions _options = options.Value;

    public (string token, DateTime expiresAt) CreateAccessToken(
        AppUser user, IEnumerable<string> permissions)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("userType", user.UserType.ToString()),
            new("fullName", user.FullName),
        };

        // One claim per permission. PermissionAuthorizationHandler iterates
        // these by claim type ("perm") rather than packing them into a single
        // delimited claim — keeps the contract trivial and lets the JWT
        // viewer (jwt.io etc.) display each permission as its own row.
        foreach (var permission in permissions)
            claims.Add(new Claim("perm", permission));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
