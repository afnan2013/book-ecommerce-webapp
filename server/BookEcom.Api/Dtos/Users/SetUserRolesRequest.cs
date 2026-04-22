using System.ComponentModel.DataAnnotations;

namespace BookEcom.Api.Dtos.Users;

public class SetUserRolesRequest
{
    public List<int> RoleIds { get; set; } = [];

    [Required]
    public string ConcurrencyStamp { get; set; } = "";
}
