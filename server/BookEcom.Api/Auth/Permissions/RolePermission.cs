using Microsoft.AspNetCore.Identity;

namespace BookEcom.Api.Auth.Permissions;

public class RolePermission
{
    public int RoleId { get; set; }
    public int PermissionId { get; set; }

    public IdentityRole<int> Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}
