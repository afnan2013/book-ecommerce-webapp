namespace BookEcom.Domain.Entities;

/// <summary>
/// Join entity linking an Identity role to a permission. The role side is an
/// ASP.NET Identity type — we deliberately don't expose a <c>Role</c> nav
/// property here so this assembly stays free of Identity dependencies. The
/// foreign key relationship is configured explicitly in Infrastructure.
/// </summary>
public class RolePermission
{
    public int RoleId { get; set; }
    public int PermissionId { get; set; }

    public Permission Permission { get; set; } = null!;
}
