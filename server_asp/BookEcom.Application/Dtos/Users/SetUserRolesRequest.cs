using System.ComponentModel.DataAnnotations;

namespace BookEcom.Application.Dtos.Users;

public class SetUserRolesRequest
{
    public List<int> RoleIds { get; set; } = [];

    [Required]
    public string ConcurrencyStamp { get; set; } = "";
}
