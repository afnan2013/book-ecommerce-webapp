using BookEcom.Api.Auth;
using BookEcom.Api.Auth.Jwt;
using BookEcom.Api.Dtos.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BookEcom.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    IJwtTokenService tokens,
    ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<LoginResponse>> Register(RegisterRequest req, CancellationToken ct)
    {
        if (req.UserType == UserType.Admin)
        {
            return BadRequest(new { error = "Admin users cannot self-register." });
        }

        var user = new AppUser
        {
            UserName = req.Email,
            Email = req.Email,
            FullName = req.FullName,
            UserType = req.UserType,
        };

        var result = await userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        logger.LogInformation("Registered {UserType} {Email}", req.UserType, req.Email);

        var (token, expiresAt) = tokens.CreateAccessToken(user);
        return Ok(new LoginResponse
        {
            AccessToken = token,
            ExpiresAt = expiresAt,
            User = ToDto(user),
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest req, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(req.Email);
        if (user is null)
        {
            return Unauthorized();
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            logger.LogInformation("Failed login for {Email}", req.Email);
            return Unauthorized();
        }

        var (token, expiresAt) = tokens.CreateAccessToken(user);
        return Ok(new LoginResponse
        {
            AccessToken = token,
            ExpiresAt = expiresAt,
            User = ToDto(user),
        });
    }

    private static UserDto ToDto(AppUser u) => new()
    {
        Id = u.Id,
        Email = u.Email ?? "",
        FullName = u.FullName,
        UserType = u.UserType,
    };
}
