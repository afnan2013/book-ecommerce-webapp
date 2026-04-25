using BookEcom.Application.Auth.Jwt;
using BookEcom.Application.Dtos.Auth;
using BookEcom.Application.Dtos.Users;
using BookEcom.Application.Permissions;
using BookEcom.Domain.Auth;
using BookEcom.Domain.Common.Results;
using Microsoft.AspNetCore.Identity;

namespace BookEcom.Application.Auth;

public class AuthService(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    IJwtTokenService tokens,
    IPermissionService permissionService,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<Result<LoginResponse>> RegisterAsync(RegisterRequest req, CancellationToken ct)
    {
        if (req.UserType == UserType.Employee)
        {
            return Result<LoginResponse>.Validation("Employee users cannot self-register.");
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

        // Auto-assign the default role for this UserType so a freshly-
        // registered user has the baseline permissions a Buyer/Seller
        // needs out of the gate. If the role is missing (admin deleted
        // it post-bootstrap, mis-configured environment) we log a
        // warning and continue — the account is still valid and an
        // admin can grant permissions manually. We don't roll back the
        // user creation; partial state is preferable to no account.
        var defaultRole = RoleNames.DefaultRoleForUserType(req.UserType);
        if (defaultRole is not null)
        {
            var addRole = await userManager.AddToRoleAsync(user, defaultRole);
            if (!addRole.Succeeded)
            {
                logger.LogWarning(
                    "Auth.Register — could not add {Email} to default role {Role}: {Errors}",
                    user.Email, defaultRole,
                    string.Join(", ", addRole.Errors.Select(e => e.Description)));
            }
        }

        logger.LogInformation("Auth.Register — registered {UserType} {Email}", req.UserType, req.Email);
        return await BuildLoginResponseAsync(user, ct);
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

        return await BuildLoginResponseAsync(user, ct);
    }

    private async Task<LoginResponse> BuildLoginResponseAsync(AppUser user, CancellationToken ct)
    {
        // Roles + effective permissions are baked into the JWT once, at
        // token issuance time, so subsequent authorization checks (and
        // [Authorize(Roles=…)] fallback usage) are claim reads — no DB
        // round-trip per request. Tradeoff: role/permission changes
        // don't take effect until the next token is issued. Phase 8E
        // (refresh tokens) will give us the invalidation seam.
        var roles = await userManager.GetRolesAsync(user);
        var permissions = await permissionService.GetEffectivePermissionsAsync(user.Id, ct);
        var (token, expiresAt) = tokens.CreateAccessToken(user, roles, permissions);

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
