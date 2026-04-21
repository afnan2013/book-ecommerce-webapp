using BookEcom.Api.Auth;

namespace BookEcom.Api.Entities;

public class UserPermission
{
    public int UserId { get; set; }
    public int PermissionId { get; set; }

    public AppUser User { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}
