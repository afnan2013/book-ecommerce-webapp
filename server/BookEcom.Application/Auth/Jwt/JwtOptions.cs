namespace BookEcom.Application.Auth.Jwt;

/// <summary>
/// Bound from configuration ("Jwt" section). Lives in Application because the
/// shape of the access token is an Application concern; the consumer of these
/// values (the signing implementation) lives in Infrastructure.
/// </summary>
public class JwtOptions
{
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public string SigningKey { get; set; } = "";
    public int AccessTokenMinutes { get; set; } = 720;
}
