using BookEcom.Api.Auth;
using BookEcom.Api.Auth.Jwt;
using BookEcom.Api.Common.Results;
using BookEcom.Api.Dtos.Auth;
using Microsoft.AspNetCore.Identity;

namespace BookEcom.Api.Application.Auth;

public class AuthService(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    IJwtTokenService tokens,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<Result<LoginResponse>> RegisterAsync(RegisterRequest req, CancellationToken ct)
    {
        if (req.UserType == UserType.Admin)
        {
            return Result<LoginResponse>.Validation("Admin users cannot self-register.");
        }

        var user = new AppUser
        {
            UserName = req.Email,
            Email = req.Email,
            FullName = req.FullName,
            UserType = req.UserType,
        };

        var created = await userManager.CreateAsync(user, req.Password);
        if (!created.Succeeded)
        {
            return Result<LoginResponse>.Validation(
                "Could not register user.",
                created.Errors.Select(e => e.Description).ToList());
        }

        logger.LogInformation("Auth.Register — registered {UserType} {Email}", req.UserType, req.Email);
        return BuildLoginResponse(user);
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest req, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(req.Email);
        if (user is null)
        {
            return Result<LoginResponse>.Unauthorized("Invalid credentials.");
        }

        var signIn = await signInManager.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: true);
        if (!signIn.Succeeded)
        {
            logger.LogInformation("Auth.Login — failed login for {Email}", req.Email);
            return Result<LoginResponse>.Unauthorized("Invalid credentials.");
        }

        return BuildLoginResponse(user);
    }

    private LoginResponse BuildLoginResponse(AppUser user)
    {
        var (token, expiresAt) = tokens.CreateAccessToken(user);
        return new LoginResponse
        {
            AccessToken = token,
            ExpiresAt = expiresAt,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? "",
                FullName = user.FullName,
                UserType = user.UserType,
            },
        };
    }
}
