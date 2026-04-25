using Microsoft.AspNetCore.Authorization;

namespace BookEcom.Application.Auth.Authorization;

/// <summary>
/// "User must have permission X." The whole policy machinery hinges on
/// pairing one of these (the question) with a
/// <see cref="PermissionAuthorizationHandler"/> (the answer).
/// </summary>
public class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
