using System.ComponentModel.DataAnnotations;

namespace BookEcom.Api.Dtos.Roles;

public class CreateRoleRequest
{
    [Required]
    [MinLength(1)]
    public string Name { get; set; } = "";
}
