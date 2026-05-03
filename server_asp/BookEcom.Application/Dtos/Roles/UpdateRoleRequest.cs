using System.ComponentModel.DataAnnotations;

namespace BookEcom.Application.Dtos.Roles;

public class UpdateRoleRequest
{
    [Required]
    [MinLength(1)]
    public string Name { get; set; } = "";
}
