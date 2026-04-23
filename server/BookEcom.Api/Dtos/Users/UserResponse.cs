using BookEcom.Api.Dtos.Permissions;
using BookEcom.Domain.Auth;

namespace BookEcom.Api.Dtos.Users;

public class UserResponse
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public UserType UserType { get; set; }
    public string ConcurrencyStamp { get; set; } = "";
    public List<UserRoleDto> Roles { get; set; } = [];
    public List<PermissionDto> DirectPermissions { get; set; } = [];
}

public class UserRoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
