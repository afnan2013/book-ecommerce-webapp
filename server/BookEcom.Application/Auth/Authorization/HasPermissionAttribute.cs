using Microsoft.AspNetCore.Authorization;

namespace BookEcom.Application.Auth.Authorization;

/// <summary>
/// Controller / action attribute that gates the endpoint on a single
/// permission. Wraps <see cref="AuthorizeAttribute"/> with a synthetic
/// policy name (<c>perm:{permission}</c>) that
/// <see cref="PermissionPolicyProvider"/> materialises into a
/// <see cref="PermissionRequirement"/> on demand. The prefix is the seam
/// between "name a policy that doesn't exist yet" (this attribute) and
/// "build a policy from that name" (the provider).
/// </summary>
public class HasPermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "perm:";

    public HasPermissionAttribute(string permission)
        : base(policy: PolicyPrefix + permission)
    {
    }
}
