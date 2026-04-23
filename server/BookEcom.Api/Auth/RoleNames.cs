namespace BookEcom.Api.Auth;

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
}
