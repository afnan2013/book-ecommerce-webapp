using BookEcom.Api.Auth;
using BookEcom.Api.Data;
using BookEcom.Domain.Auth;
using BookEcom.Domain.Common.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookEcom.Api.Application.Users.Policies;

/// <summary>
/// Business rule: the system must always retain at least one SuperAdmin.
/// Lifted out of <c>UsersController</c> (where the rule was duplicated in
/// both Delete and SetRoles) so there's exactly one place it lives — the
/// textbook SRP / specification-pattern demonstration. Callers inspect the
/// returned <see cref="Result"/> rather than catching exceptions.
/// </summary>
public class LastSuperAdminPolicy(
    UserManager<AppUser> userManager,
    RoleManager<IdentityRole<int>> roleManager,
    AppDbContext db)
{
    /// <summary>
    /// Succeeds unless <paramref name="user"/> is a SuperAdmin and would be
    /// the last one after deletion.
    /// </summary>
    public async Task<Result> CanDeleteAsync(AppUser user, CancellationToken ct)
    {
        var userRoles = await userManager.GetRolesAsync(user);
        if (!userRoles.Any(RoleNames.IsSuperAdmin)) return Result.Success();

        return await EnsureNotLastSuperAdminAsync(
            "Cannot delete the last SuperAdmin user.", ct);
    }

    /// <summary>
    /// Succeeds unless <paramref name="user"/> is a SuperAdmin, the requested
    /// role set does NOT include SuperAdmin, and they would be the last
    /// SuperAdmin if demoted.
    /// </summary>
    public async Task<Result> CanDemoteAsync(
        AppUser user,
        IReadOnlyCollection<IdentityRole<int>> newRoles,
        CancellationToken ct)
    {
        var currentRoles = await userManager.GetRolesAsync(user);
        var isSuperAdmin = currentRoles.Any(RoleNames.IsSuperAdmin);
        if (!isSuperAdmin) return Result.Success();

        var keepsSuperAdmin = newRoles.Any(r => RoleNames.IsSuperAdmin(r.NormalizedName));
        if (keepsSuperAdmin) return Result.Success();

        return await EnsureNotLastSuperAdminAsync(
            "Cannot remove SuperAdmin role from the last SuperAdmin user.", ct);
    }

    private async Task<Result> EnsureNotLastSuperAdminAsync(string message, CancellationToken ct)
    {
        var superAdminRole = await roleManager.FindByNameAsync(RoleNames.SuperAdmin);
        if (superAdminRole is null) return Result.Success();

        var count = await db.UserRoles.CountAsync(ur => ur.RoleId == superAdminRole.Id, ct);
        return count <= 1 ? Result.Validation(message) : Result.Success();
    }
}
