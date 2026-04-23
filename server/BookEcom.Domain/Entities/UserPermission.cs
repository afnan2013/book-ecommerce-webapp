namespace BookEcom.Domain.Entities;

/// <summary>
/// Join entity for a permission granted directly to a user (bypassing roles).
/// The user side is an ASP.NET Identity type — we deliberately don't expose a
/// <c>User</c> nav property here so this assembly stays free of Identity
/// dependencies. The foreign key relationship is configured explicitly in
/// Infrastructure.
/// </summary>
public class UserPermission
{
    public int UserId { get; set; }
    public int PermissionId { get; set; }

    public Permission Permission { get; set; } = null!;
}
