using BookEcom.Api.Dtos.Permissions;

namespace BookEcom.Api.Dtos.Roles;

public class RoleResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string ConcurrencyStamp { get; set; } = "";
    public List<PermissionDto> Permissions { get; set; } = [];
}
