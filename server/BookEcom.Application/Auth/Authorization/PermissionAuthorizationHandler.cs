using Microsoft.AspNetCore.Authorization;

namespace BookEcom.Application.Auth.Authorization;

/// <summary>
/// Resolves a <see cref="PermissionRequirement"/> by looking for a matching
/// <c>perm</c> claim on the calling user. The claims were baked into the
/// JWT at login time by <c>AuthService</c> via
/// <c>IPermissionService.GetEffectivePermissionsAsync</c>, so this handler
/// is a pure claim read — no DB round-trip per request.
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    public const string PermissionClaimType = "perm";

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.HasClaim(PermissionClaimType, requirement.Permission))
            context.Succeed(requirement);

        // Don't call Fail() — other handlers may grant access via different
        // requirements on the same policy. Failing silently lets the
        // framework's default "deny if no Succeed" behaviour kick in.
        return Task.CompletedTask;
    }
}
