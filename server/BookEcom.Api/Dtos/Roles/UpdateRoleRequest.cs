using System.ComponentModel.DataAnnotations;

namespace BookEcom.Api.Dtos.Roles;

public class UpdateRoleRequest
{
    [Required]
    [MinLength(1)]
    public string Name { get; set; } = "";
}
