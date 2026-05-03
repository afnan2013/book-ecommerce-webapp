using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace BookEcom.Application.Auth.Authorization;

/// <summary>
/// Materialises authorization policies for any name starting with
/// <see cref="HasPermissionAttribute.PolicyPrefix"/>. Without this provider,
/// every permission would need its own <c>builder.Services.AddAuthorization
/// (o => o.AddPolicy("perm:books.read", …))</c> call in <c>Program.cs</c> —
/// brittle, easy to forget. With it, adding a new permission to
/// <c>PermissionNames</c> is a one-line change with zero DI plumbing.
///
/// Policies that don't match the prefix delegate to the framework's default
/// provider so any future <c>AddPolicy(...)</c> calls keep working.
/// </summary>
public class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
        _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
        _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(HasPermissionAttribute.PolicyPrefix, StringComparison.Ordinal))
        {
            var permission = policyName[HasPermissionAttribute.PolicyPrefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }
}
