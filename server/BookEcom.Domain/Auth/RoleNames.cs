namespace BookEcom.Domain.Auth;

/// <summary>
/// Code-referenced role names. Roles are normally managed through the admin
/// API — this class only contains roles the application itself has to reason
/// about by name.
/// </summary>
public static class RoleNames
{
    /// <summary>
    /// The protected "god" role. Always exists, always has every permission
    /// in the catalog, and cannot be deleted or modified through the admin
    /// API. The first admin created by the seeder is assigned this role.
    ///
    /// Without this guarantee, a misclick in the admin UI could revoke every
    /// permission and lock everyone out.
    /// </summary>
    public const string SuperAdmin = "SuperAdmin";

    /// <summary>
    /// Identity-normalized form of <see cref="SuperAdmin"/>. ASP.NET Identity
    /// upper-cases role names for lookup — use this when querying
    /// <c>AspNetRoles.NormalizedName</c> directly on the DbSet.
    /// </summary>
    public const string SuperAdminNormalized = "SUPERADMIN";

    /// <summary>
    /// Canonical SuperAdmin check. Ordinal case-insensitive so it matches both
    /// the display form ("SuperAdmin") and the Identity-normalized form
    /// ("SUPERADMIN"). The one place this comparison lives — everything else
    /// calls through here.
    /// </summary>
    public static bool IsSuperAdmin(string? name) =>
        string.Equals(name, SuperAdmin, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Default role assigned to a self-registered user. Bootstrapped on a
    /// fresh DB by <c>IdentitySeeder</c>; <c>AuthService.RegisterAsync</c>
    /// looks up the name returned here and adds the new user to it.
    /// Returns <c>null</c> for <see cref="UserType.Employee"/> because admins
    /// can't self-register (blocked higher up in <c>RegisterAsync</c>).
    /// </summary>
    public const string Buyer = "Buyer";

    /// <inheritdoc cref="Buyer"/>
    public const string Seller = "Seller";
    public const string Employee = "Employee";
    /// <summary>
    /// Maps a self-registered <see cref="UserType"/> to the default role
    /// they should land in. Returning <c>null</c> means "no default" — the
    /// caller should leave the user role-less. The Employee branch is
    /// theoretically reachable here but practically dead-code: employee
    /// self-registration is rejected before this is consulted.
    /// </summary>
    public static string? DefaultRoleForUserType(UserType userType) => userType switch
    {
        UserType.Buyer => Buyer,
        UserType.Seller => Seller,
        UserType.Employee => Employee,
        _ => null,
    };
}
