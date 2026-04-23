using System.ComponentModel.DataAnnotations;

namespace BookEcom.Application.Dtos.Users;

public class SetUserPermissionsRequest
{
    public List<int> PermissionIds { get; set; } = [];

    [Required]
    public string ConcurrencyStamp { get; set; } = "";
}
